using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.Assets;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SauronSheet.Frontend.Pages.Categories;

[Authorize]
[ValidateAntiForgeryToken]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<IndexModel> _logger;

    public List<CategoryDto> Categories { get; set; } = new();
    public IReadOnlyList<string> AvailableIcons => AllowedBootstrapIcons.GetAllIconsForDropdown();

    // Not [BindProperty]: each handler reads its own scoped form fields directly from
    // Request.Form via BindXxxFormFromRequest() helpers, avoiding cross-form ModelState
    // pollution between the two forms on this page.
    public CreateCategoryInputModel CreateForm { get; set; } = new();
    public EditCategoryInputModel EditForm { get; set; } = new();

    public IndexModel(IMediator mediator, ILogger<IndexModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _mediator.Send(new GetCategoriesQuery());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnGetAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Read directly from the form collection — avoids the MVC model binding pipeline
            // so validation is scoped to only the fields this form actually sends,
            // eliminating cross-form ModelState pollution.
            var input = BindCreateFormFromRequest();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, true))
            {
                var errorMessage = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                Sentry.SentrySdk.CaptureMessage(
                    "Category create input validation failed",
                    scope =>
                    {
                        scope.SetTag("page", "Categories/Index.OnPostCreateAsync");
                        scope.SetTag("validation_error_count", validationResults.Count.ToString());
                        scope.Level = Sentry.SentryLevel.Info;
                    });
                return new JsonResult(new { success = false, error = errorMessage },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            if (!AllowedBootstrapIcons.IsValid(input.IconName))
                return BadRequest(new { error = "Invalid icon selection" });

            var categoryType = input.Type!.Value == 0 ? CategoryType.Income : CategoryType.Expense;

            var command = new CreateCategoryCommand(
                input.Name,
                categoryType,
                input.Color ?? "#3498DB",
                input.IconName);

            var result = await _mediator.Send(command);

            return new JsonResult(new { success = true, categoryId = result });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category creation failed: {Message}", ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostCreateAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostCreateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while creating the category" },
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

            // Read directly from the form collection — avoids the MVC model binding pipeline
            // so validation is scoped to only the fields this form actually sends,
            // eliminating cross-form ModelState pollution.
            var input = BindEditFormFromRequest();

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, true))
            {
                var errorMessage = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                Sentry.SentrySdk.CaptureMessage(
                    "Category edit input validation failed",
                    scope =>
                    {
                        scope.SetTag("page", "Categories/Index.OnPostUpdateAsync");
                        scope.SetTag("validation_error_count", validationResults.Count.ToString());
                        scope.Level = Sentry.SentryLevel.Info;
                    });
                return new JsonResult(new { success = false, error = errorMessage },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            if (input.CategoryId == Guid.Empty)
            {
                return new JsonResult(new { success = false, error = "Invalid category ID" },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            var command = new RenameCategoryCommand(
                input.CategoryId,
                input.Name);

            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category update failed: {Message}", ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostUpdateAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostUpdateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while updating the category" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (categoryId == Guid.Empty)
            {
                return new JsonResult(new { success = false, error = "Invalid category ID" },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            var command = new DeleteCategoryCommand(categoryId);
            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category deletion failed: {Message}", ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostDeleteAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category");
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Categories/Index.OnPostDeleteAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while deleting the category" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    // Form-reading helpers — read scoped fields directly from Request.Form to avoid
    // cross-form ModelState pollution when two [BindProperty] forms share a page.

    private CreateCategoryInputModel BindCreateFormFromRequest() =>
        new CreateCategoryInputModel
        {
            Name = Request.Form["CreateForm.Name"].FirstOrDefault()?.Trim() ?? string.Empty,
            Type = int.TryParse(Request.Form["CreateForm.Type"].FirstOrDefault(), out int t) ? t : null,
            Color = Request.Form["CreateForm.Color"].FirstOrDefault() ?? "#3498DB",
            IconName = Request.Form["CreateForm.IconName"].FirstOrDefault() ?? "tag"
        };

    private EditCategoryInputModel BindEditFormFromRequest() =>
        new EditCategoryInputModel
        {
            CategoryId = Guid.TryParse(Request.Form["EditForm.CategoryId"].FirstOrDefault(), out Guid id)
                ? id
                : Guid.Empty,
            Name = Request.Form["EditForm.Name"].FirstOrDefault()?.Trim() ?? string.Empty,
            Color = Request.Form["EditForm.Color"].FirstOrDefault() ?? "#3498DB",
            IconName = Request.Form["EditForm.IconName"].FirstOrDefault() ?? "tag"
        };
}

/// <summary>
/// Input model for the create-category form.
/// Intentionally separate from <see cref="EditCategoryInputModel"/> so that the two forms
/// on the same Razor Page do not share a validated type and cause cross-form ModelState pollution.
/// Type is nullable so that [Required] triggers when the field is missing,
/// and [Range(0, 1)] ensures the value is a valid <see cref="CategoryType"/>.
/// </summary>
public class CreateCategoryInputModel
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required")]
    [Range(0, 1, ErrorMessage = "Type must be 0 (Income) or 1 (Expense)")]
    public int? Type { get; set; }

    [Required]
    public string Color { get; set; } = "#3498DB";

    [Required]
    public string IconName { get; set; } = "tag";
}

/// <summary>
/// Input model for the rename/edit-category form.
/// Only contains the fields the edit operation actually requires,
/// preventing false validation failures when the create form is empty.
/// </summary>
public class EditCategoryInputModel
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    // Color and IconName are sent by the edit form UI but are not consumed by RenameCategoryCommand.
    // They are captured here to avoid unrecognised-field warnings during model binding.
    public string Color { get; set; } = "#3498DB";
    public string IconName { get; set; } = "tag";
}
