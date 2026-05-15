using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Auth;

/// <summary>
/// Register page model.
/// Handles new user registration via MediatR RegisterUserCommand.
/// Auto-logs in user after successful registration.
/// </summary>
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public RegisterModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        try
        {
            // Register new user
            await _mediator.Send(
                new RegisterUserCommand(Input.Email, Input.Password, Input.ConfirmPassword));

            // Auto-login after successful registration
            var loginResult = await _mediator.Send(
                new LoginUserCommand(Input.Email, Input.Password));

            // Set JWT cookies
            Response.Cookies.Append(
                "sb-access-token",
                loginResult.AccessToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Path = "/",
                    Expires = loginResult.ExpiresAt
                });

            Response.Cookies.Append(
                "sb-refresh-token",
                loginResult.RefreshToken,
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

            return RedirectToPage("/dashboard");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Auth/Register.OnPostAsync");
                scope.SetTag("register.email", Input.Email);
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Auth/Register.OnPostAsync");
                scope.SetTag("register.email", Input.Email);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Auth/Register.OnPostAsync");
                scope.SetTag("register.email", Input.Email);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An error occurred during registration. Please try again later.";
            return Page();
        }
    }
}

public class RegisterInputModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
