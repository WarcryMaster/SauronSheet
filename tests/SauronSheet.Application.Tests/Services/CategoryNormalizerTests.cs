namespace SauronSheet.Application.Tests.Services;

using SauronSheet.Application.Services;
using Xunit;

/// <summary>
/// TDD RED cycle for CategoryNormalizer.
/// Spec: PCE-2a (diacritics), PCE-2b (casing), PCE-2c (combined).
/// CategoryNormalizer does NOT exist yet — all tests will fail to compile.
/// </summary>
[Trait("Category", "Application")]
public class CategoryNormalizerTests
{
    // ── PCE-2a: Diacritics normalization ─────────────────────────────────────

    [Fact]
    public void Normalize_WithAccentedVowel_ReturnsSameKeyAsUnaccented()
    {
        // PCE-2a: "Alimentación" and "Alimentacion" must produce the same normalized key.
        var withAccent = CategoryNormalizer.Normalize("Alimentación");
        var withoutAccent = CategoryNormalizer.Normalize("Alimentacion");

        Assert.NotNull(withAccent);
        Assert.Equal(withAccent, withoutAccent);
    }

    [Fact]
    public void Normalize_WithMultipleDiacritics_ReturnsSameKeyAsStripped()
    {
        // PCE-2a triangulation: different accented word to confirm generalization.
        var withAccents = CategoryNormalizer.Normalize("Café");
        var withoutAccents = CategoryNormalizer.Normalize("Cafe");

        Assert.NotNull(withAccents);
        Assert.Equal(withAccents, withoutAccents);
    }

    // ── PCE-2b: Case insensitivity ────────────────────────────────────────────

    [Fact]
    public void Normalize_UpperCase_ReturnsSameKeyAsLowerCase()
    {
        // PCE-2b: "COMPRAS" and "compras" must produce the same normalized key.
        var upper = CategoryNormalizer.Normalize("COMPRAS");
        var lower = CategoryNormalizer.Normalize("compras");

        Assert.NotNull(upper);
        Assert.Equal(upper, lower);
    }

    [Fact]
    public void Normalize_MixedCase_ReturnsSameKeyAsLowerCase()
    {
        // PCE-2b triangulation: mixed case to confirm case folding is general.
        var mixed = CategoryNormalizer.Normalize("CoMpRaS");
        var lower = CategoryNormalizer.Normalize("compras");

        Assert.NotNull(mixed);
        Assert.Equal(mixed, lower);
    }

    // ── PCE-2c: Diacritics + casing combined ─────────────────────────────────

    [Fact]
    public void Normalize_CombinedDiacriticsAndCasing_ReturnsSameKey()
    {
        // PCE-2c: "ALIMENTACIÓN", "Alimentacion", "alimentación" → same key.
        var a = CategoryNormalizer.Normalize("ALIMENTACIÓN");
        var b = CategoryNormalizer.Normalize("Alimentacion");
        var c = CategoryNormalizer.Normalize("alimentación");

        Assert.NotNull(a);
        Assert.Equal(a, b);
        Assert.Equal(b, c);
    }

    [Fact]
    public void Normalize_SpanishEnie_RemovesOrNormalizes()
    {
        // PCE-2c triangulation: ñ is a common Spanish character requiring normalization.
        // Both "Compañia" and "Compania" must produce the same key.
        var withEnie = CategoryNormalizer.Normalize("Compañia");
        var withoutEnie = CategoryNormalizer.Normalize("Compania");

        Assert.NotNull(withEnie);
        Assert.Equal(withEnie, withoutEnie);
    }

    // ── Null/whitespace edge cases ────────────────────────────────────────────

    [Fact]
    public void Normalize_NullInput_ReturnsNull()
    {
        var result = CategoryNormalizer.Normalize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Normalize_WhitespaceInput_ReturnsNull()
    {
        var result = CategoryNormalizer.Normalize("   ");

        Assert.Null(result);
    }

    [Fact]
    public void Normalize_EmptyString_ReturnsNull()
    {
        var result = CategoryNormalizer.Normalize(string.Empty);

        Assert.Null(result);
    }

    // ── Output contract: lowercase + trimmed ─────────────────────────────────

    [Fact]
    public void Normalize_ValidInput_ReturnsLowercaseAndTrimmed()
    {
        // The normalized key must be lowercase and trimmed — deterministic for DB storage.
        var result = CategoryNormalizer.Normalize("  Alimentación  ");

        Assert.NotNull(result);
        Assert.Equal("alimentacion", result);
    }

    [Fact]
    public void Normalize_SimpleAsciiValue_ReturnsTrimmedLowercase()
    {
        // Triangulation: ASCII input without diacritics → straightforward lowercase+trim.
        var result = CategoryNormalizer.Normalize("  Compras  ");

        Assert.NotNull(result);
        Assert.Equal("compras", result);
    }
}
