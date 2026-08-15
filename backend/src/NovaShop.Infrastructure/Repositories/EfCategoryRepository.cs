using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfCategoryRepository : ICategoryRepository
{
    private readonly NovaShopDbContext _context;

    public EfCategoryRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Category>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Categories.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Category>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Category>> GetByCategoryNameAsync(string name)
    {
        return await _context.Categories
            .Where(c => c.Name.Contains(name))
            .ToListAsync();
    }

    public async Task<List<Category>> GetSubCategoriesAsync(int parentCategoryId)
    {
        return await _context.Categories
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category.Id;
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await GetByIdAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Categories.AnyAsync(c => c.Id == id);
    }
}
