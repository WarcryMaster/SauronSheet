using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Subcategories.Commands;
using SauronSheet.Application.Features.Subcategories.Queries;
using SauronSheet.Application.Features.Subcategories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SauronSheet.Frontend.Pages.Categories;

[Authorize]
[ValidateAntiForgeryToken]
public class SubcategoriesModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubcategoriesModel> _logger;

    public List<SubcategoryDto> Subcategories { get; set; } = new();
    public CategoryDto? ParentCategory { get; set; }

    public CreateSubcategoryInputModel CreateForm { get; set; } = new();
    public EditSubcategoryInputModel EditForm { get; set; } = new();

    public SubcategoriesModel(IMediator mediator, ILogger<SubcategoriesModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid? categoryId)
    {
        var id = categoryId.HasValue && categoryId.Value != Guid.Empty
            ? categoryId.Value
            : Guid.TryParse(Request.Query["categoryId"], out var q) ? q : Guid.Empty;

        if (id == Guid.Empty)
            return RedirectToPage("/Categories/Index");

        try
        {
            var allCategories = await _mediator.Send(new GetCategoriesQuery());
            ParentCategory = allCategories.FirstOrDefault(c => c.Id == id);

            if (ParentCategory == null)
                return RedirectToPage("/Categories/Index");

            Subcategories = await _mediator.Send(new GetSubcategoriesByCategoryQuery(id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading subcategories for category {CategoryId}", id);
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Subcategories.OnGetAsync");
                scope.SetTag("category_id", id.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid? categoryId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var id = categoryId.HasValue && categoryId.Value != Guid.Empty
                ? categoryId.Value
                : Guid.TryParse(Request.Query["categoryId"], out var q) ? q : Guid.Empty;

            if (id == Guid.Empty)
                return new JsonResult(new { success = false, error = "Invalid category ID" },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var input = BindCreateFormFromRequest();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, true))
            {
                var errorMessage = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                Sentry.SentrySdk.CaptureMessage(
                    "Subcategory create input validation failed",
                    scope =>
                    {
                        scope.SetTag("page", "Categories/Subcategories.OnPostCreateAsync");
                        scope.SetTag("validation_error_count", validationResults.Count.ToString());
                        scope.Level = Sentry.SentryLevel.Info;
                    });
                return new JsonResult(new { success = false, error = errorMessage },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            var command = new CreateSubcategoryCommand(id, input.Name);
            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogInformation("Subcategory creation failed: {Message}", ex.Message);
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subcategory");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Subcategories.OnPostCreateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while creating the subcategory" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var input = BindEditFormFromRequest();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, true))
            {
                var errorMessage = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                Sentry.SentrySdk.CaptureMessage(
                    "Subcategory edit input validation failed",
                    scope =>
                    {
                        scope.SetTag("page", "Categories/Subcategories.OnPostUpdateAsync");
                        scope.SetTag("validation_error_count", validationResults.Count.ToString());
                        scope.Level = Sentry.SentryLevel.Info;
                    });
                return new JsonResult(new { success = false, error = errorMessage },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            if (input.SubcategoryId == Guid.Empty)
            {
                return new JsonResult(new { success = false, error = "Invalid subcategory ID" },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            var command = new RenameSubcategoryCommand(input.SubcategoryId, input.Name);
            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogInformation("Subcategory update failed: {Message}", ex.Message);
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subcategory");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Subcategories.OnPostUpdateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while updating the subcategory" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid subcategoryId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (subcategoryId == Guid.Empty)
            {
                return new JsonResult(new { success = false, error = "Invalid subcategory ID" },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            var command = new DeleteSubcategoryCommand(subcategoryId);
            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogInformation("Subcategory deletion failed: {Message}", ex.Message);
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subcategory");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Subcategories.OnPostDeleteAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while deleting the subcategory" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    private CreateSubcategoryInputModel BindCreateFormFromRequest() =>
        new CreateSubcategoryInputModel
        {
            Name = Request.Form["CreateForm.Name"].FirstOrDefault()?.Trim() ?? string.Empty
        };

    private EditSubcategoryInputModel BindEditFormFromRequest() =>
        new EditSubcategoryInputModel
        {
            SubcategoryId = Guid.TryParse(Request.Form["EditForm.SubcategoryId"].FirstOrDefault(), out Guid id)
                ? id
                : Guid.Empty,
            Name = Request.Form["EditForm.Name"].FirstOrDefault()?.Trim() ?? string.Empty
        };
}

public class CreateSubcategoryInputModel
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

public class EditSubcategoryInputModel
{
    [Required]
    public Guid SubcategoryId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
