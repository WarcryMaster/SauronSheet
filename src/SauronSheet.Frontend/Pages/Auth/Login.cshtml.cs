using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using Microsoft.Extensions.Logging;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Application.Features.Auth.DTOs;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Auth;

/// <summary>
/// Login page model.
/// Handles user login via MediatR LoginUserCommand.
/// Sets JWT cookies on successful login.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public LoginModel(IMediator mediator, ILogger<LoginModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Dashboard";

        var email = Input?.Email;
        var password = Input?.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login: Missing email or password");
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login: ModelState is invalid");
            return Page();
        }

        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", email);

            var result = await _mediator.Send(
                new LoginUserCommand(email, password));

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

            _logger.LogInformation("Login successful for email: {Email}", email);
            return LocalRedirect(ReturnUrl);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed for email {Email}: Unauthorized - {Message}", email, ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("auth.email", email ?? "");
                scope.SetTag("auth.stage", "login");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Login failed for email {Email}: Domain error - {Message}", email, ex.Message);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("auth.email", email ?? "");
                scope.SetTag("auth.stage", "login");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}: Network error", email);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("auth.email", email ?? "");
                scope.SetTag("auth.stage", "login");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}: Unexpected error", email);
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("auth.email", email ?? "");
                scope.SetTag("auth.stage", "login");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An error occurred during login. Please try again later.";
            return Page();
        }
    }
}

public class LoginInputModel
{
    public string? Email { get; set; } = null;
    public string? Password { get; set; } = null;
}
