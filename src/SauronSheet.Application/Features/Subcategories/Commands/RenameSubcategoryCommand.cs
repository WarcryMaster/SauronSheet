namespace SauronSheet.Application.Features.Subcategories.Commands;

using MediatR;

public record RenameSubcategoryCommand(
    Guid SubcategoryId,
    string NewName) : IRequest<Unit>;
