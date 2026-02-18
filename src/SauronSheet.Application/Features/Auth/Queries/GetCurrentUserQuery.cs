namespace SauronSheet.Application.Features.Auth.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Get current user profile query.
/// Requires authentication - does NOT implement IAnonymousRequest.
/// </summary>
public record GetCurrentUserQuery : IRequest<UserProfileDto>;
