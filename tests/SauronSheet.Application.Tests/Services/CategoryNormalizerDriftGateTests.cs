namespace SauronSheet.Application.Tests.Services;

using SauronSheet.Application.Services;
using Xunit;

/// <summary>
/// Task 1.5 — Drift gate: verifies that CategoryNormalizer.Normalize() output
/// matches the SQL backfill strategy used in migration 011 (lower(trim(name))).
///
/// If a future change to CategoryNormalizer diverges from lower(trim(name))
/// for existing DB rows, this test will catch it and block the PR merge.
///
/// Rows are the exact categories and subcategories present in the DB at the
/// time migration 011 was designed (verified via direct Supabase query).
/// All existing names are pure ASCII — no diacritics — so both approaches
/// must produce identical output.
/// </summary>
[Trait("Category", "Application")]
public class CategoryNormalizerDriftGateTests
{
    // ── Known existing category rows from public.categories ──────────────────
    // Verified via: SELECT name, lower(trim(name)) FROM public.categories ORDER BY name
    private static readonly (string name, string sqlNormalized)[] ExistingCategoryRows =
    [
        ("educacion",            "educacion"),
        ("hogar",                "hogar"),
        ("movimientos excluidos","movimientos excluidos"),
        ("vehiculo y transporte","vehiculo y transporte"),
    ];

    // ── Known existing subcategory rows from public.subcategories ────────────
    // Verified via: SELECT name, lower(trim(name)) FROM public.subcategories ORDER BY name
    private static readonly (string name, string sqlNormalized)[] ExistingSubcategoryRows =
    [
        ("agua",                          "agua"),
        ("farmacia, herbolario y nutricion","farmacia, herbolario y nutricion"),
        ("gasolina y combustible",        "gasolina y combustible"),
        ("luz y gas",                     "luz y gas"),
        ("mantenimiento de vehiculo",     "mantenimiento de vehiculo"),
        ("traspaso entre cuentas",        "traspaso entre cuentas"),
    ];

    [Fact]
    public void Normalize_ExistingCategoryNames_MatchesSqlBackfill()
    {
        // For EVERY known category row: C# output MUST equal the SQL backfill value.
        // Any divergence here means migration 011 backfill will produce wrong keys
        // and normalized_name won't match what the application computes at runtime.
        foreach (var (name, sqlNormalized) in ExistingCategoryRows)
        {
            var csharpOutput = CategoryNormalizer.Normalize(name);

            Assert.NotNull(csharpOutput);
            Assert.True(sqlNormalized == csharpOutput,
                $"Drift detected for category '{name}': SQL='{sqlNormalized}', C#='{csharpOutput}'");
        }
    }

    [Fact]
    public void Normalize_ExistingSubcategoryNames_MatchesSqlBackfill()
    {
        // Same drift check for subcategory rows.
        foreach (var (name, sqlNormalized) in ExistingSubcategoryRows)
        {
            var csharpOutput = CategoryNormalizer.Normalize(name);

            Assert.NotNull(csharpOutput);
            Assert.True(sqlNormalized == csharpOutput,
                $"Drift detected for subcategory '{name}': SQL='{sqlNormalized}', C#='{csharpOutput}'");
        }
    }

    [Fact]
    public void Normalize_AllExistingRows_CoversBothTables()
    {
        // Triangulation: verify we're actually checking all rows, not an empty set.
        Assert.Equal(4, ExistingCategoryRows.Length);
        Assert.Equal(6, ExistingSubcategoryRows.Length);
    }
}
