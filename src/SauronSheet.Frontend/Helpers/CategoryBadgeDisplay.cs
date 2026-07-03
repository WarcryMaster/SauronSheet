namespace SauronSheet.Frontend.Helpers;

public sealed record CategoryBadgeDisplay(
    string PrimaryText,
    string? SecondaryText = null,
    bool IsUncategorized = false,
    bool UsesWarningStyle = false,
    string? AccentColor = null,
    bool IsSystemCategory = false,
    string? SystemCategorySlug = null);
