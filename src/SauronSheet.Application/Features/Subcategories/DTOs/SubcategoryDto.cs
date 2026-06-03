namespace SauronSheet.Application.Features.Subcategories.DTOs;

using System;

public record SubcategoryDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    bool IsAutoCreated,
    int TransactionCount = 0);
