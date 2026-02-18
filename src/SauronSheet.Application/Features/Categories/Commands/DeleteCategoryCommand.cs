namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;

public record DeleteCategoryCommand(
    Guid CategoryId) : IRequest<Unit>;
