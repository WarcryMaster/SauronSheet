namespace SauronSheet.Application.Features.Subcategories.Queries;

using DTOs;
using MediatR;
using System;
using System.Collections.Generic;

public record GetSubcategoriesByCategoryQuery(Guid CategoryId) : IRequest<List<SubcategoryDto>>;
