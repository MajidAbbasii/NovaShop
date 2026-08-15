using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetAllAsync(
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? onlyAvailable = null,
        int pageNumber = 1,
        int pageSize = 12,
        int? categoryId = null);

    Task<Product?> GetByIdAsync(int id);
    Task<int> AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
