namespace SauronSheet.Application.Features.Auth.Commands;

using Domain.Exceptions;
using Domain.Services;
using MediatR;

/// <summary>
/// Handler for ResendConfirmationEmailCommand.
/// Calls IAuthService.ResendConfirmationEmailAsync.
/// </summary>
public class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand, Unit>
{
    private readonly IAuthService _authService;

    public ResendConfirmationEmailCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Unit> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResendConfirmationEmailAsync(request.Email);

        if (!result.IsSuccess)
            throw new DomainException(result.ErrorMessage ?? "Unable to resend confirmation email.");

        return Unit.Value;
    }
}
