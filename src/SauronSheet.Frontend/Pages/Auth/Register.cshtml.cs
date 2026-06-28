using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MediatR;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Application.Features.Auth.DTOs;
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
    private readonly ILogger<RegisterModel> _logger;

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool ShowResendConfirmation { get; set; }

    public RegisterModel(IMediator mediator, ILogger<RegisterModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public void OnGet()
    {
        var pendingEmail = TempData["PendingConfirmationEmail"] as string;
        if (!string.IsNullOrWhiteSpace(pendingEmail))
        {
            Input.Email = pendingEmail;
            ShowResendConfirmation = true;
        }

        SuccessMessage = TempData["SuccessMessage"] as string;
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
            RegistrationResultDto registrationResult = await _mediator.Send(
                new RegisterUserCommand(Input.Email, Input.Password, Input.ConfirmPassword));

            if (registrationResult.RequiresEmailConfirmation)
            {
                TempData["SuccessMessage"] = "Registration completed. Check your email to confirm your account.";
                TempData["PendingConfirmationEmail"] = Input.Email;
                return RedirectToPage("/auth/register");
            }

            // Auto-login after successful registration
            AuthTokenDto loginResult = await _mediator.Send(
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
            _logger.LogInformation("Registration failed for email {Email}: {Message}", Input.Email, ex.Message);
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

    public async Task<IActionResult> OnPostResendConfirmationAsync(string? resendEmail)
    {
        var emailToResend = string.IsNullOrWhiteSpace(resendEmail) ? Input.Email : resendEmail;
        ShowResendConfirmation = true;

        if (string.IsNullOrWhiteSpace(emailToResend))
        {
            ErrorMessage = "Email is required to resend confirmation.";
            return Page();
        }

        Input.Email = emailToResend;

        try
        {
            await _mediator.Send(new ResendConfirmationEmailCommand(emailToResend));
            SuccessMessage = "Confirmation email resent. Please check your inbox.";
            return Page();
        }
        catch (DomainException ex)
        {
            _logger.LogInformation("Resend confirmation failed for email {Email}: {Message}", emailToResend, ex.Message);
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Auth/Register.OnPostResendConfirmationAsync");
                scope.SetTag("register.email", emailToResend);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred while resending confirmation email. Please try again.";
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Auth/Register.OnPostResendConfirmationAsync");
                scope.SetTag("register.email", emailToResend);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while resending confirmation email.";
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
