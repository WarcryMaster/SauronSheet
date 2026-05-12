namespace SauronSheet.Application.Features.Auth.Queries;

using SauronSheet.Domain.Common;
using Domain.Services;
using Domain.Exceptions;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetCurrentUserQuery.
/// Retrieves current user profile from IUserContext and IAuthService.
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IAuthService _authService;
    private readonly IUserContext _userContext;

    public GetCurrentUserQueryHandler(IAuthService authService, IUserContext userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    public async Task<UserProfileDto> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var profile = await _authService.GetUserProfileAsync(_userContext.UserId);

        if (profile == null)
            throw new EntityNotFoundException("User", _userContext.UserId);

        return new UserProfileDto(
            profile.Id.Value,
            profile.Email,
            profile.DisplayName,
            profile.CreatedAt);
    }
}
