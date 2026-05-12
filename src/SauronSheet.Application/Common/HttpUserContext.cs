namespace SauronSheet.Application.Common;

using Microsoft.AspNetCore.Http;
using SauronSheet.Domain.Common;

/// <summary>
/// HTTP User Context Implementation.
/// Implements IUserContext from Domain layer.
/// Extracts current user information from HttpContext claims set by JwtCookieMiddleware.
/// </summary>
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            return userId;
        }
    }

    public string UserEmail =>
        _httpContextAccessor.HttpContext?.User
            ?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
