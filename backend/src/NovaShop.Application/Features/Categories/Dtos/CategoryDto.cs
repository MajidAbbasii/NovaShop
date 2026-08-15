namespace NovaShop.Application.Features.Categories.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}
