namespace NovaShop.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public List<Product> Products { get; private set; } = new();
    public List<Category> SubCategories { get; private set; } = new();
}
