using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Frontend.Pages.Categories;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Categories;

/// <summary>
/// Tests for the /categories IndexModel page.
/// Covers:
/// - Model validation separation (EditCategoryInputModel vs CreateCategoryInputModel)
/// - Handler routing and MediatR dispatch
/// - Error handling and Sentry-safe response shapes
/// </summary>
public class IndexModelTests
{
    // ---------------------------------------------------------------------------
    // Model validation tests — directly prove the dual-form separation fix.
    // These do NOT need HTTP infrastructure and run fast.
    // ---------------------------------------------------------------------------

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_WithValidCategoryIdAndName_PassesValidation()
    {
        // Arrange — only the fields the edit form actually sends
        var input = new EditCategoryInputModel
        {
            CategoryId = Guid.NewGuid(),
            Name = "Groceries"
        };

        // Act
        var results = ValidateModel(input);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_WithoutType_PassesValidation()
    {
        // The key regression test: editing a category must NOT require Type.
        // The old CategoryFormModel required Type (int with default 1), and the
        // edit modal does not send a Type field, causing false validation failures.
        var input = new EditCategoryInputModel
        {
            CategoryId = Guid.NewGuid(),
            Name = "Transport"
            // Type is intentionally absent — EditCategoryInputModel should not declare it
        };

        var results = ValidateModel(input);

        // Edit model has no Type property, so no validation errors from a missing Type
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_WithEmptyName_FailsValidation()
    {
        var input = new EditCategoryInputModel
        {
            CategoryId = Guid.NewGuid(),
            Name = string.Empty
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EditCategoryInputModel.Name)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_WithNameExceedingMaxLength_FailsValidation()
    {
        var input = new EditCategoryInputModel
        {
            CategoryId = Guid.NewGuid(),
            Name = new string('X', 51) // 51 chars > max 50
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EditCategoryInputModel.Name)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithAllRequiredFields_PassesValidation()
    {
        var input = new CreateCategoryInputModel
        {
            Name = "Food",
            Type = 1,
            Color = "#E74C3C",
            IconName = "utensils"
        };

        var results = ValidateModel(input);

        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithNullType_FailsValidation()
    {
        // Regression: missing Type must NOT silently default to Expense.
        // Type is now int? — null must be rejected by [Required].
        var input = new CreateCategoryInputModel
        {
            Name = "Food",
            Type = null, // missing
            Color = "#E74C3C",
            IconName = "utensils"
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryInputModel.Type)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithInvalidTypeValue_FailsValidation()
    {
        // Regression: invalid Type (e.g. 99) must be rejected, not silently
        // coerced to Expense.
        var input = new CreateCategoryInputModel
        {
            Name = "Food",
            Type = 99, // out of range
            Color = "#E74C3C",
            IconName = "utensils"
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryInputModel.Type)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithTypeZero_PassesValidation()
    {
        // 0 (Income) is a valid Type value
        var input = new CreateCategoryInputModel
        {
            Name = "Salary",
            Type = 0,
            Color = "#27AE60",
            IconName = "wallet"
        };

        var results = ValidateModel(input);

        Assert.Empty(results);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithEmptyName_FailsValidation()
    {
        var input = new CreateCategoryInputModel
        {
            Name = string.Empty,
            Type = 1,
            Color = "#E74C3C",
            IconName = "utensils"
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryInputModel.Name)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void CreateCategoryInputModel_WithEmptyIconName_FailsValidation()
    {
        var input = new CreateCategoryInputModel
        {
            Name = "Food",
            Type = 1,
            Color = "#E74C3C",
            IconName = string.Empty
        };

        var results = ValidateModel(input);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCategoryInputModel.IconName)));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_DoesNotHaveTypeProperty()
    {
        // Structural guard: EditCategoryInputModel must not declare a Type property.
        // Having Type would re-introduce the validation coupling we are fixing.
        var property = typeof(EditCategoryInputModel).GetProperty("Type");

        Assert.Null(property);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void EditCategoryInputModel_IsNotSameTypeAsCreateCategoryInputModel()
    {
        // Guard against accidentally reverting to a single shared form model.
        Assert.NotEqual(typeof(CreateCategoryInputModel), typeof(EditCategoryInputModel));
    }

    // ---------------------------------------------------------------------------
    // Handler tests — verify MediatR dispatch and JSON response shapes.
    // The handler reads from Request.Form directly (no MVC binder machinery needed).
    // ---------------------------------------------------------------------------

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUpdateAsync_WithValidInput_DispatchesRenameCategoryCommand()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(m => m.Send(
                It.Is<RenameCategoryCommand>(c => c.CategoryId == categoryId && c.NewName == "Updated Name"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var formData = new Dictionary<string, StringValues>
        {
            { "EditForm.CategoryId", categoryId.ToString() },
            { "EditForm.Name", "Updated Name" },
            { "EditForm.Color", "#3498DB" },
            { "EditForm.IconName", "tag" }
        };
        var model = CreateModelWithForm(mockMediator, formData);

        // Act
        var result = await model.OnPostUpdateAsync();

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":true", payload);

        mockMediator.Verify(m => m.Send(
            It.Is<RenameCategoryCommand>(c => c.CategoryId == categoryId && c.NewName == "Updated Name"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUpdateAsync_WithEmptyCategoryId_ReturnsSuccessFalseWithoutDispatch()
    {
        // Regression: Guid.Empty must NOT be dispatched to MediatR.
        var mockMediator = new Mock<IMediator>();

        var formData = new Dictionary<string, StringValues>
        {
            { "EditForm.CategoryId", Guid.Empty.ToString() },
            { "EditForm.Name", "Test" },
        };
        var model = CreateModelWithForm(mockMediator, formData);

        // Act
        var result = await model.OnPostUpdateAsync();

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);

        mockMediator.Verify(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUpdateAsync_WithNonParseableCategoryId_ReturnsSuccessFalseWithoutDispatch()
    {
        // Regression: a non-GUID string in the form field must not produce a valid ID.
        var mockMediator = new Mock<IMediator>();

        var formData = new Dictionary<string, StringValues>
        {
            { "EditForm.CategoryId", "not-a-guid" },
            { "EditForm.Name", "Test" },
        };
        var model = CreateModelWithForm(mockMediator, formData);

        // Act
        var result = await model.OnPostUpdateAsync();

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);

        mockMediator.Verify(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUpdateAsync_WithEmptyName_ReturnsSuccessFalse()
    {
        // Arrange — name is intentionally empty to trigger validation failure
        var categoryId = Guid.NewGuid();
        var mockMediator = new Mock<IMediator>();

        var formData = new Dictionary<string, StringValues>
        {
            { "EditForm.CategoryId", categoryId.ToString() },
            { "EditForm.Name", string.Empty },
        };
        var model = CreateModelWithForm(mockMediator, formData);

        // Act
        var result = await model.OnPostUpdateAsync();

        // Assert — validation should catch empty name, no MediatR call
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);

        mockMediator.Verify(m => m.Send(It.IsAny<RenameCategoryCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUpdateAsync_WhenDomainExceptionThrown_ReturnsSuccessFalseWithMessage()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(m => m.Send(It.IsAny<RenameCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Category name already in use"));

        var formData = new Dictionary<string, StringValues>
        {
            { "EditForm.CategoryId", categoryId.ToString() },
            { "EditForm.Name", "Duplicate Name" },
        };
        var model = CreateModelWithForm(mockMediator, formData);

        // Act
        var result = await model.OnPostUpdateAsync();

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);
        Assert.Contains("Category name already in use", payload);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostDeleteAsync_WithValidCategoryId_DispatchesDeleteCategoryCommand()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(m => m.Send(
                It.Is<DeleteCategoryCommand>(c => c.CategoryId == categoryId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var model = CreateModelWithForm(mockMediator, new Dictionary<string, StringValues>());

        // Act
        var result = await model.OnPostDeleteAsync(categoryId);

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":true", payload);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostDeleteAsync_WithEmptyCategoryId_ReturnsSuccessFalseWithoutDispatch()
    {
        // Regression: Guid.Empty must NOT be dispatched to MediatR.
        var mockMediator = new Mock<IMediator>();

        var model = CreateModelWithForm(mockMediator, new Dictionary<string, StringValues>());

        // Act
        var result = await model.OnPostDeleteAsync(Guid.Empty);

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);

        mockMediator.Verify(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostDeleteAsync_WhenDomainExceptionThrown_ReturnsSuccessFalse()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(m => m.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Cannot delete system default category"));

        var model = CreateModelWithForm(mockMediator, new Dictionary<string, StringValues>());

        // Act
        var result = await model.OnPostDeleteAsync(categoryId);

        // Assert
        var json = Assert.IsType<JsonResult>(result);
        var payload = SerializeAnonymous(json.Value!);
        Assert.Contains("\"success\":false", payload);
        Assert.Contains("Cannot delete system default category", payload);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static IndexModel CreateModelWithForm(
        Mock<IMediator> mediator,
        Dictionary<string, StringValues> formValues)
    {
        var httpContext = new DefaultHttpContext();

        // Set up a logged-in user
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "test-user-id") },
            "TestAuth"));

        // Populate Request.Form so BindXxxFormFromRequest() helpers can read it
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(formValues);

        var pageContext = new PageContext(new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));

        return new IndexModel(
            mediator.Object,
            Mock.Of<ILogger<IndexModel>>())
        {
            PageContext = pageContext
        };
    }

    private static string SerializeAnonymous(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
}
