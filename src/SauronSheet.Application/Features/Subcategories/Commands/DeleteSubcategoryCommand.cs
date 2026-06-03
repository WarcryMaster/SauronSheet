namespace SauronSheet.Application.Features.Subcategories.Commands;

using MediatR;

public record DeleteSubcategoryCommand(
    Guid SubcategoryId) : IRequest<Unit>;
