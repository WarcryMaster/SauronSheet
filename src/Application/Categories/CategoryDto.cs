namespace Application.Categories;

/// <summary>
/// Category data transfer object
/// </summary>
public class CategoryDto : Common.BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
