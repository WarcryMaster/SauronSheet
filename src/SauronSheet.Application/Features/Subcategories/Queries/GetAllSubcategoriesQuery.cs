namespace SauronSheet.Application.Features.Subcategories.Queries;

using System;
using System.Collections.Generic;
using MediatR;
using SauronSheet.Application.Features.Subcategories.DTOs;

/// <summary>
/// Returns all subcategories for the current user, grouped by category.
/// Used by forms that need to populate dependent category/subcategory dropdowns.
/// </summary>
public record GetAllSubcategoriesQuery : IRequest<List<SubcategoryDto>>;
