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
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<IndexModel> _logger;

    public List<CategoryDto> Categories { get; set; } = new();
    public IReadOnlyList<string> AvailableIcons => AllowedBootstrapIcons.GetAllIconsForDropdown();

    [BindProperty]
    public CategoryFormModel CreateForm { get; set; } = new();

    [BindProperty]
    public CategoryFormModel EditForm { get; set; } = new();

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
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Categories/Index.OnGetAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate icon
            if (!AllowedBootstrapIcons.IsValid(CreateForm.IconName))
                return BadRequest(new { error = "Invalid icon selection" });

            var command = new CreateCategoryCommand(
                CreateForm.Name,
                CreateForm.Color ?? "#3498DB",
                CreateForm.IconName);

            var result = await _mediator.Send(command);

            return new JsonResult(new { success = true, categoryId = result });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category creation failed: {Message}", ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Categories/Index.OnPostCreateAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return new JsonResult(new { success = false, error = ex.Message }, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Categories/Index.OnPostCreateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while creating the category" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate icon
            if (!AllowedBootstrapIcons.IsValid(EditForm.IconName))
                return BadRequest(new { error = "Invalid icon selection" });

            var command = new RenameCategoryCommand(
                EditForm.CategoryId,
                EditForm.Name);

            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category update failed: {Message}", ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Categories/Index.OnPostUpdateAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category");
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Categories/Index.OnPostUpdateAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            return new JsonResult(new { success = false, error = "An error occurred while updating the category" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new DeleteCategoryCommand(categoryId);
            await _mediator.Send(command);

            return new JsonResult(new { success = true });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Category deletion failed: {Message}", ex.Message);
            return new JsonResult(new { success = false, error = ex.Message },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category");
            return new JsonResult(new { success = false, error = "An error occurred while deleting the category" },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}

public class CategoryFormModel
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required")]
    public int Type { get; set; } = 1; // Default to Expense

    [Required]
    public string Color { get; set; } = "#3498DB";

    [Required]
    public string IconName { get; set; } = "tag";

    public Guid CategoryId { get; set; }
}
