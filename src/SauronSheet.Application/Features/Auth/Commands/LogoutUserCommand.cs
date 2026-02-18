namespace SauronSheet.Application.Features.Auth.Commands;

using MediatR;

/// <summary>
/// Logout a user command.
/// Requires authentication - does NOT implement IAnonymousRequest.
/// </summary>
public record LogoutUserCommand(
    string AccessToken) : IRequest<Unit>;
