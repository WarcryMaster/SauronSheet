namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using DTOs;
using MediatR;

/// <summary>
/// Handler for RefreshTokenCommand.
/// Calls IAuthService.RefreshTokenAsync with refresh token, returns new AuthTokenDto.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthTokenDto> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.IsSuccess)
            throw new UnauthorizedAccessException("Session expired.");

        return new AuthTokenDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            result.UserId!.Value);
    }
}
