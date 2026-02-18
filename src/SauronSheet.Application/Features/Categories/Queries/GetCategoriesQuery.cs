namespace SauronSheet.Application.Features.Categories.Queries;

using DTOs;
using MediatR;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
