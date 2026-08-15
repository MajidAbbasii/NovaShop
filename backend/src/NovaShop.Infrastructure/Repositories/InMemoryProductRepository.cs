using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new()
    {
        new() { Id = 1, Name = "Apple MacBook Pro 16-inch M4", Price = 24_990_000, OriginalPrice = 27_990_000, ImageUrl = "https://picsum.photos/id/20/600/600", Rating = 4.8, Stock = 15 },
        new() { Id = 2, Name = "Sony WH-1000XM5 Headphones", Price = 3_980_000, OriginalPrice = 4_490_000, ImageUrl = "https://picsum.photos/id/60/600/600", Rating = 4.9, Stock = 23 },
        new() { Id = 3, Name = "Samsung Galaxy Watch 7", Price = 2_990_000, OriginalPrice = 3_290_000, ImageUrl = "https://picsum.photos/id/201/600/600", Rating = 4.6, Stock = 8 },
    };

    public async Task<PagedResult<Product>> GetAllAsync(string? searchTerm = null,
                                                        decimal? minPrice = null,
                                                        decimal? maxPrice = null,
                                                        bool? onlyAvailable = null,
                                                        int pageNumber = 1,
                                                        int pageSize = 12,
                                                        int? categoryId = null)
    {
        var query = _products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (onlyAvailable == true)
            query = query.Where(p => p.IsAvailable);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<int> AddAsync(Product product)
    {
        product.Id = _products.Max(p => p.Id) + 1;
        _products.Add(product);
        return product.Id;
    }

    public async Task UpdateAsync(Product product)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.OriginalPrice = product.OriginalPrice;
            existing.Description = product.Description;
            existing.ImageUrl = product.ImageUrl;
            existing.Stock = product.Stock;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product != null)
            _products.Remove(product);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _products.Any(p => p.Id == id);
    }
}
