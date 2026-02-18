using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.DTOs;
using SauronSheet.Application.Features.Auth.Queries;

namespace SauronSheet.Frontend.Pages;

/// <summary>
/// Dashboard page model.
/// Protected page requiring authentication.
/// Displays current user profile and placeholder content for Phase 4 analytics.
/// </summary>
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public UserProfileDto? UserProfile { get; set; }

    public DashboardModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            UserProfile = await _mediator.Send(new GetCurrentUserQuery());
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/Auth/Login");
        }
    }
}
