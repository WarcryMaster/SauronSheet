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

    [Column("is_auto_created")]
    public bool IsAutoCreated { get; set; }
}
