namespace Application.Categories.Queries;

using MediatR;

/// <summary>
/// Query to retrieve categories for the current user
/// </summary>
public record GetCategoriesQuery(Guid UserId) : IRequest<GetCategoriesQueryDto>;
