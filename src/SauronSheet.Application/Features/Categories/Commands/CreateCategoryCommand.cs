namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;
using SauronSheet.Domain.ValueObjects;

public record CreateCategoryCommand(
    string Name,
    CategoryType Type,
    string? Color = null,
    string? Icon = null) : IRequest<Guid>;
