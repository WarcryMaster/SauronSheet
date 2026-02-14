namespace Application.Categories.Commands;

using MediatR;

/// <summary>
/// Command to create a new expense category
/// </summary>
public record CreateCategoryCommand(Guid UserId, string Name, string? Description) : IRequest<CategoryDto>;
