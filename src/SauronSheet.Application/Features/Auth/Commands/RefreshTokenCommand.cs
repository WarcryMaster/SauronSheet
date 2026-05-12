namespace SauronSheet.Application.Features.Auth.Commands;

using SauronSheet.Application.Common;
using DTOs;
using MediatR;

/// <summary>
/// Refresh token command.
/// Implements IAnonymousRequest - refresh token is provided instead of access token.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken) : IRequest<AuthTokenDto>, IAnonymousRequest;
