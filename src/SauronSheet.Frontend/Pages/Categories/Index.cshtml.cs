using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace SauronSheet.Frontend.Pages.Categories;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public CreateCategoryInputModel? NewCategory { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            await _mediator.Send(new CreateCategoryCommand(
                NewCategory!.Name,
                NewCategory.Color,
                NewCategory.Icon));
            SuccessMessage = "Category created successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while creating the category.";
            // TODO Phase 6: Log exception
        }

        Categories = await _mediator.Send(new GetCategoriesQuery());
        return Page();
    }

    public async Task<IActionResult> OnPostRenameAsync(Guid categoryId, string newName)
    {
        try
        {
            await _mediator.Send(new RenameCategoryCommand(categoryId, newName));
            SuccessMessage = "Category renamed successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while renaming the category.";
            // TODO Phase 6: Log exception
        }

        Categories = await _mediator.Send(new GetCategoriesQuery());
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid categoryId)
    {
        try
        {
            await _mediator.Send(new DeleteCategoryCommand(categoryId));
            SuccessMessage = "Category deleted successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred while deleting the category.";
            // TODO Phase 6: Log exception
        }

        Categories = await _mediator.Send(new GetCategoriesQuery());
        return Page();
    }
}

public class CreateCategoryInputModel
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Icon { get; set; }
}
