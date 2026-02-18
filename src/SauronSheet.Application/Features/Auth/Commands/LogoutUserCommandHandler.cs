namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Services;
using MediatR;

/// <summary>
/// Handler for LogoutUserCommand.
/// Calls IAuthService.LogoutAsync to revoke the session.
/// </summary>
public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Unit>
{
    private readonly IAuthService _authService;

    public LogoutUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Unit> Handle(
        LogoutUserCommand request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.AccessToken);
        return Unit.Value;
    }
}
