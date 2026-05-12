namespace SauronSheet.Application.Features.Auth.Commands;

using SauronSheet.Application.Common;
using DTOs;
using MediatR;

/// <summary>
/// Register a new user command.
/// Implements IAnonymousRequest - does not require authentication.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<RegistrationResultDto>, IAnonymousRequest;
