namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;

public record RenameCategoryCommand(
    Guid CategoryId,
    string NewName) : IRequest<Unit>;
