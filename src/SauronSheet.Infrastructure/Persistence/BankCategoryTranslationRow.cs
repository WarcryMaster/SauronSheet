namespace SauronSheet.Infrastructure.Persistence;

using Postgrest.Attributes;
using Postgrest.Models;

/// <summary>
/// Postgrest DTO for the bank_category_translations table (read-only).
/// Allows users to map raw bank category/subcategory values to resolved display names.
/// </summary>
[Table("bank_category_translations")]
internal class BankCategoryTranslationRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("bank_category")]
    public string BankCategory { get; set; } = "";

    [Column("bank_subcategory")]
    public string? BankSubcategory { get; set; }

    [Column("resolved_category_name")]
    public string ResolvedCategoryName { get; set; } = "";

    [Column("resolved_subcategory_name")]
    public string? ResolvedSubcategoryName { get; set; }
}
