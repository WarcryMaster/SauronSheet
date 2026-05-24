namespace SauronSheet.Application.Features.Categories.DTOs;

/// <summary>
/// Data Transfer Object for Category.
/// Used to return category data to frontend/API consumers.
/// </summary>
public record CategoryDto(
    Guid Id,
    string Name,
    string Type,
    string Color,
    string IconName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int TransactionCount = 0);
