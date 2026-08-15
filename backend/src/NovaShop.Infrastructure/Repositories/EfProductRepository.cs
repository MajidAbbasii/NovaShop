using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfProductRepository : IProductRepository
{
    private readonly NovaShopDbContext _context;

    public EfProductRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Product>> GetAllAsync(
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? onlyAvailable = null,
        int pageNumber = 1,
        int pageSize = 12,
        int? categoryId = null)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        if (onlyAvailable.HasValue && onlyAvailable.Value)
        {
            query = query.Where(p => p.Stock > 0);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Include(p => p.Category)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Colors)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Products.AnyAsync(p => p.Id == id);
    }
}