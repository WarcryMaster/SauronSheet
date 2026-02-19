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

    public SupabaseAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Supabase:Key is not configured.");

        // Set the apikey header required by Supabase REST API
        _httpClient.DefaultRequestHeaders.Remove("apikey");
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var payload = new { email, password };
            var response = await _httpClient.PostAsJsonAsync(
                "auth/v1/signup",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(errorContent);
                var message = jsonDoc.RootElement.TryGetProperty("error_description", out var errorDesc)
                    ? errorDesc.GetString() ?? "Registration failed"
                    : jsonDoc.RootElement.TryGetProperty("msg", out var msg)
                        ? msg.GetString() ?? "Registration failed"
                        : "Registration failed";
                return AuthResult.Failure(message);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var userId = root.GetProperty("user").GetProperty("id").GetString() ?? "";
            // Supabase may return null session if email confirmation is required
            if (!root.TryGetProperty("session", out var session) || session.ValueKind == JsonValueKind.Null)
            {
                return AuthResult.Failure("Registration successful. Please check your email to confirm your account.");
            }

            var accessToken = session.GetProperty("access_token").GetString() ?? "";
            var refreshToken = session.GetProperty("refresh_token").GetString() ?? "";
            var expiresIn = session.GetProperty("expires_in").GetInt32();

            return AuthResult.Success(
                new UserId(userId),
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddSeconds(expiresIn));
        }
        catch (Exception ex)
        {
            return AuthResult.Failure(ex.Message);
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

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure("Invalid email or password.");

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var userId = root.GetProperty("user").GetProperty("id").GetString() ?? "";
            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            return AuthResult.Success(
                new UserId(userId),
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddSeconds(expiresIn));
        }
        catch
        {
            return AuthResult.Failure("Invalid email or password.");
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

            if (!response.IsSuccessStatusCode)
                return AuthResult.Failure("Session expired.");

            var jsonContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var userId = root.GetProperty("user").GetProperty("id").GetString() ?? "";
            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var newRefreshToken = root.GetProperty("refresh_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            return AuthResult.Success(
                new UserId(userId),
                accessToken,
                newRefreshToken,
                DateTime.UtcNow.AddSeconds(expiresIn));
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

            var id = root.GetProperty("id").GetString() ?? "";
            var email = root.GetProperty("email").GetString() ?? "";

            string? displayName = null;
            if (root.TryGetProperty("user_metadata", out var metadata) &&
                metadata.TryGetProperty("display_name", out var name))
            {
                displayName = name.GetString();
            }

            return new UserProfile(
                new UserId(id),
                email,
                displayName,
                DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }
}
