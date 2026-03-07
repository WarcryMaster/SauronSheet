using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SauronSheet.Frontend.Pages;

/// <summary>
/// Index page model.
/// Redirects unauthenticated users to /Auth/Login and authenticated users to /Dashboard.
/// Acts as the entry point for the application.
/// </summary>
[AllowAnonymous]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // If user is authenticated, go to Dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard");
        }

        // If not authenticated, go to Login
        return RedirectToPage("/Auth/Login");
    }
}
