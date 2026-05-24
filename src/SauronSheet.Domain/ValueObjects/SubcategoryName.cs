namespace SauronSheet.Domain.ValueObjects;

using System;
using Exceptions;

/// <summary>
/// Strong-typed value object for subcategory name.
/// Enforces business rule: 1-50 characters, non-empty after trim.
/// </summary>
public record SubcategoryName(string Value)
{
    public const int MinLength = 1;
    public const int MaxLength = 50;

    /// <summary>
    /// Factory method to create a validated SubcategoryName.
    /// Throws DomainException if validation fails.
    /// </summary>
    public static SubcategoryName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Subcategory name is required.");
        }

        var trimmed = name.Trim();

        if (trimmed.Length < MinLength)
        {
            throw new DomainException($"Subcategory name must be at least {MinLength} character.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Subcategory name must not exceed {MaxLength} characters.");
        }

        return new SubcategoryName(trimmed);
    }

    /// <summary>
    /// Validates that the value meets length constraints.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Value)) return false;
        if (Value.Length < MinLength || Value.Length > MaxLength) return false;
        return true;
    }
}
