namespace SauronSheet.Infrastructure.Persistence;

using Postgrest.Attributes;
using Postgrest.Models;

/// <summary>
/// Postgrest DTO for the subcategories table.
/// </summary>
[Table("subcategories")]
internal class SubcategoryRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("category_id")]
    public string CategoryId { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Normalized deduplication key for this subcategory name.
    /// Computed by CategoryNormalizer.Normalize(Name); stored via migration 011.
    /// NOT NULL in DB after migration 011.
    /// </summary>
    [Column("normalized_name")]
    public string NormalizedName { get; set; } = "";

    [Column("is_auto_created")]
    public bool IsAutoCreated { get; set; }
}
