namespace SauronSheet.Application.Features.Auth.Commands;

using SauronSheet.Application.Common;
using MediatR;

/// <summary>
/// Resend account confirmation email for pending signup users.
/// Implements IAnonymousRequest - does not require authentication.
/// </summary>
public record ResendConfirmationEmailCommand(string Email) : IRequest<Unit>, IAnonymousRequest;
