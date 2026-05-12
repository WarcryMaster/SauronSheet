namespace SauronSheet.Application.Features.Auth.Commands;

using SauronSheet.Application.Common;
using DTOs;
using MediatR;

/// <summary>
/// Login a user command.
/// Implements IAnonymousRequest - does not require authentication.
/// </summary>
public record LoginUserCommand(
    string Email,
    string Password) : IRequest<AuthTokenDto>, IAnonymousRequest;
