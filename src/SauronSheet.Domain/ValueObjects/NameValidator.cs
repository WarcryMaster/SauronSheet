namespace SauronSheet.Domain.ValueObjects;

using System;
using Exceptions;

/// <summary>
/// Shared validation logic for name value objects (CategoryName, SubcategoryName, etc.).
/// Enforces business rule: 1-50 characters, non-empty after trim.
/// </summary>
internal static class NameValidator
{
    internal const int MinLength = 1;
    internal const int MaxLength = 50;

    /// <summary>
    /// Validates and trims a name value.
    /// </summary>
    /// <param name="name">Raw input to validate.</param>
    /// <param name="fieldName">Display name for error messages (e.g., "Category name").</param>
    /// <returns>The trimmed name.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    internal static string Validate(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        string trimmed = name.Trim();

        if (trimmed.Length < MinLength)
        {
            throw new DomainException($"{fieldName} must be at least {MinLength} character.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"{fieldName} must not exceed {MaxLength} characters.");
        }

        return trimmed;
    }
}
