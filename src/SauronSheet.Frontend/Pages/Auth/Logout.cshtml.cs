using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;

namespace SauronSheet.Frontend.Pages.Auth;

/// <summary>
/// Logout page model.
/// Handles user logout via MediatR LogoutUserCommand.
/// Clears JWT cookies and revokes session.
/// Note: Logout is POST-only (CSRF safe) - no OnGet method.
/// </summary>
public class LogoutModel : PageModel
{
    private readonly IMediator _mediator;

    public LogoutModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = Request.Cookies["sb-access-token"];

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                await _mediator.Send(new LogoutUserCommand(token));
            }
            catch
            {
                // Logout errors are non-fatal; session may already be expired
            }
        }

        // Clear cookies
        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");

        return RedirectToPage("/auth/login");
    }
}
