namespace SauronSheet.Domain.ValueObjects;

using System;
using System.Text.RegularExpressions;
using Exceptions;

/// <summary>
/// Strong-typed value object for hexadecimal color codes.
/// Enforces business rule: valid hex format #RRGGBB (uppercase).
/// </summary>
public record ColorHex(string Value)
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-F]{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Factory method to create a validated ColorHex.
    /// Normalizes input to uppercase and validates regex format.
    /// Throws DomainException if validation fails.
    /// </summary>
    public static ColorHex Create(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            throw new DomainException("Color hex code is required.");
        }

        var normalized = hex.ToUpperInvariant().Trim();

        if (!HexColorRegex.IsMatch(normalized))
        {
            throw new DomainException("Color must be valid hex code (format: #RRGGBB, e.g., #F39C12).");
        }

        return new ColorHex(normalized);
    }

    /// <summary>
    /// Validates that the value meets hex format constraints.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Value)) return false;
        return HexColorRegex.IsMatch(Value);
    }
}
