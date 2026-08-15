using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = new()
    {
        new() { Id = 1, Name = "Electronics", Description = "Electronic devices", ImageUrl = "https://picsum.photos/id/10/600/600" },
        new() { Id = 2, Name = "Clothing", Description = "Apparel and accessories", ImageUrl = "https://picsum.photos/id/11/600/600" },
        new() { Id = 3, Name = "Books", Description = "Books and educational materials", ImageUrl = "https://picsum.photos/id/12/600/600" },
    };

    public async Task<PagedResult<Category>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _categories.AsQueryable();

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Category>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return _categories.FirstOrDefault(c => c.Id == id);
    }

    public async Task<List<Category>> GetByCategoryNameAsync(string name)
    {
        return _categories.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<Category>> GetSubCategoriesAsync(int parentCategoryId)
    {
        return _categories.Where(c => c.ParentCategoryId == parentCategoryId).ToList();
    }

    public async Task<int> AddAsync(Category category)
    {
        category.Id = _categories.Max(c => c.Id) + 1;
        _categories.Add(category);
        return category.Id;
    }

    public async Task UpdateAsync(Category category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing != null)
        {
            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.ImageUrl = category.ImageUrl;
            existing.ParentCategoryId = category.ParentCategoryId;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category != null)
            _categories.Remove(category);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _categories.Any(c => c.Id == id);
    }
}
