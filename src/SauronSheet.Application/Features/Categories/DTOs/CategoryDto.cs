namespace SauronSheet.Application.Features.Categories.DTOs;

/// <summary>
/// CRITICAL FIX I-4: TransactionCount property added
/// </summary>
public record CategoryDto(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    bool IsSystemDefault,
    int TransactionCount);
