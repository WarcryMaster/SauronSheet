using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Application.Features.Auth.DTOs;

namespace SauronSheet.Frontend.Pages.Auth;

/// <summary>
/// Login page model.
/// Handles user login via MediatR LoginUserCommand.
/// Sets JWT cookies on successful login.
/// </summary>
public class LoginModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public LoginModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var result = await _mediator.Send(
                new LoginUserCommand(Input.Email, Input.Password));

            // Set JWT access token cookie
            Response.Cookies.Append(
                "sb-access-token",
                result.AccessToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Path = "/",
                    Expires = result.ExpiresAt
                });

            // Set refresh token cookie
            Response.Cookies.Append(
                "sb-refresh-token",
                result.RefreshToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

            return LocalRedirect(ReturnUrl);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}

public class LoginInputModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
