namespace SauronSheet.Application.Features.Subcategories.Commands;

using MediatR;

public record CreateSubcategoryCommand(
    Guid CategoryId,
    string Name) : IRequest<Guid>;
