namespace SauronSheet.Application.Services;

using Domain.ValueObjects;

/// <summary>
/// Result of the bank category resolution process.
/// Contains the resolved CategoryId (if found), SubcategoryId (if found),
/// and the CategorySource that describes how the resolution was determined.
/// </summary>
public record ResolutionResult(
    CategoryId? CategoryId,
    SubcategoryId? SubcategoryId,
    CategorySource CategorySource);
