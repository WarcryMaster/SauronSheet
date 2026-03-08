namespace SauronSheet.Infrastructure.Auth;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Supabase Auth Service Implementation.
/// Implements IAuthService from Domain layer.
/// Calls Supabase Auth REST API endpoints for authentication operations.
/// HttpClient.BaseAddress is pre-configured to the Supabase URL via DI.
/// </summary>
public class SupabaseAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseKey;
    private readonly string _siteUrl;

    public SupabaseAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Supabase:Key is not configured.");
        _siteUrl = configuration["Supabase:SiteUrl"] ?? "https://localhost:54099";

        // Set the apikey header required by Supabase REST API
        _httpClient.DefaultRequestHeaders.Remove("apikey");
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var payload = new
            {
                email,
                password,
                options = new
                {
                    emailRedirectTo = $"{_siteUrl}/Auth/Login"
                }
            };
            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/signup",
                payload);

            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Failure(ExtractErrorMessage(jsonContent, "Registration failed"));
            }

            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Supabase returns two formats:
            // 1. With session (email confirmation OFF): { "user": { "id": "..." }, "session": { ... } }
            // 2. Without session (email confirmation ON): { "id": "...", "email": "...", "confirmation_sent_at": "..." }
            string userId;

            if (root.TryGetProperty("user", out var userElement) &&
                userElement.TryGetProperty("id", out var nestedIdElement))
            {
                // Format 1: user nested inside "user" property
                userId = nestedIdElement.GetString() ?? "";
            }
            else if (root.TryGetProperty("id", out var directIdElement))
            {
                // Format 2: user object returned directly at root
                userId = directIdElement.GetString() ?? "";
            }
            else
            {
                return AuthResult.Failure("Registration response missing user ID.");
            }

            // Check if email confirmation is required (no session returned)
            if (root.TryGetProperty("confirmation_sent_at", out _))
            {
                return AuthResult.Failure("Registration successful! Please check your email to confirm your account.");
            }

            // Extract session if available
            if (!root.TryGetProperty("session", out var session) || session.ValueKind == JsonValueKind.Null)
            {
                return AuthResult.Failure("Registration successful! Please check your email to confirm your account.");
            }

            // Extract tokens from session
            if (!session.TryGetProperty("access_token", out var atElement) ||
                !session.TryGetProperty("refresh_token", out var rtElement) ||
                !session.TryGetProperty("expires_in", out var exElement))
            {
                return AuthResult.Failure("Registration successful but session is incomplete.");
            }

            var accessToken = atElement.GetString() ?? "";
            var refreshToken = rtElement.GetString() ?? "";
            var expiresIn = exElement.GetInt32();

            return AuthResult.Success(
                new UserId(userId),
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddSeconds(expiresIn));
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("service", "SupabaseAuthService.RegisterAsync");
                scope.SetTag("register.email", email);
                scope.Level = Sentry.SentryLevel.Error;
            });
            return AuthResult.Failure($"Registration error: {ex.Message}");
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var payload = new
            {
                email,
                password,
                grant_type = "password"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/token?grant_type=password",
                payload);

            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure(ExtractErrorMessage(jsonContent, "Invalid email or password."));

            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("user", out var userEl) ||
                !userEl.TryGetProperty("id", out var idEl))
                return AuthResult.Failure("Login response missing user data.");

            var userId = idEl.GetString() ?? "";

            if (!root.TryGetProperty("access_token", out var atEl) ||
                !root.TryGetProperty("refresh_token", out var rtEl) ||
                !root.TryGetProperty("expires_in", out var exEl))
                return AuthResult.Failure("Login response missing token data.");

            return AuthResult.Success(
                new UserId(userId),
                atEl.GetString() ?? "",
                rtEl.GetString() ?? "",
                DateTime.UtcNow.AddSeconds(exEl.GetInt32()));
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("service", "SupabaseAuthService.LoginAsync");
                scope.SetTag("login.email", email);
                scope.Level = Sentry.SentryLevel.Error;
            });
            return AuthResult.Failure($"Login error: {ex.Message}");
        }
    }

    public async Task LogoutAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/logout");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            await _httpClient.SendAsync(request);
        }
        catch
        {
            // Logout errors are non-fatal; session may already be expired
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var payload = new
            {
                refresh_token = refreshToken,
                grant_type = "refresh_token"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/token?grant_type=refresh_token",
                payload);

            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure("Session expired.");

            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("user", out var userEl) ||
                !userEl.TryGetProperty("id", out var idEl))
                return AuthResult.Failure("Refresh response missing user data.");

            if (!root.TryGetProperty("access_token", out var atEl) ||
                !root.TryGetProperty("refresh_token", out var rtEl) ||
                !root.TryGetProperty("expires_in", out var exEl))
                return AuthResult.Failure("Refresh response missing token data.");

            return AuthResult.Success(
                new UserId(idEl.GetString() ?? ""),
                atEl.GetString() ?? "",
                rtEl.GetString() ?? "",
                DateTime.UtcNow.AddSeconds(exEl.GetInt32()));
        }
        catch
        {
            return AuthResult.Failure("Session expired.");
        }
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync("auth/v1/user");
            if (!response.IsSuccessStatusCode)
                return null;

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("id", out var idEl) ||
                !root.TryGetProperty("email", out var emailEl))
                return null;

            string? displayName = null;
            if (root.TryGetProperty("user_metadata", out var metadata) &&
                metadata.TryGetProperty("display_name", out var name))
            {
                displayName = name.GetString();
            }

            return new UserProfile(
                new UserId(idEl.GetString() ?? ""),
                emailEl.GetString() ?? "",
                displayName,
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts a human-readable error message from a Supabase error JSON response.
    /// </summary>
    private static string ExtractErrorMessage(string jsonContent, string fallback)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("error_description", out var desc) && desc.ValueKind == JsonValueKind.String)
                return desc.GetString() ?? fallback;
            if (root.TryGetProperty("msg", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString() ?? fallback;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? fallback;
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                return error.GetString() ?? fallback;

            return fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
