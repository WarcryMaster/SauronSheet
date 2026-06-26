namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Strong-typed value object for subcategory name.
/// Enforces business rule: 1-50 characters, non-empty after trim.
/// Validation logic shared with <see cref="NameValidator"/>.
/// </summary>
public record SubcategoryName(string Value)
{
    public const int MinLength = NameValidator.MinLength;
    public const int MaxLength = NameValidator.MaxLength;

    /// <summary>
    /// Factory method to create a validated SubcategoryName.
    /// Throws DomainException if validation fails.
    /// </summary>
    public static SubcategoryName Create(string name)
    {
        return new SubcategoryName(NameValidator.Validate(name, "Subcategory name"));
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
