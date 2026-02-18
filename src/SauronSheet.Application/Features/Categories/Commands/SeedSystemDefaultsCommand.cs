namespace SauronSheet.Application.Features.Categories.Commands;

using MediatR;

public record SeedSystemDefaultsCommand : IRequest<List<Guid>>;
