using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SauronSheet.Frontend.Pages;

/// <summary>
/// Health check page model for Phase 0.
/// Demonstrates that Razor Pages pipeline is working.
/// No MediatR calls or authentication required in Phase 0.
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // No MediatR calls in Phase 0 — health check only
    }
}
