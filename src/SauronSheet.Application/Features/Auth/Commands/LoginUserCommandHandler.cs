namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using DTOs;
using MediatR;

/// <summary>
/// Handler for LoginUserCommand.
/// Calls IAuthService.LoginAsync, returns AuthTokenDto.
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;

    public LoginUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthTokenDto> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.IsSuccess)
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthTokenDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            result.UserId!.Value);
    }
}
