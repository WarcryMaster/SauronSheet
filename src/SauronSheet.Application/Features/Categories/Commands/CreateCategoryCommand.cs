namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;

public record CreateCategoryCommand(
    string Name,
    string? Color = null,
    string? Icon = null) : IRequest<Guid>;
