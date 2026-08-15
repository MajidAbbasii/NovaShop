using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface ICategoryRepository
{
    Task<PagedResult<Category>> GetAllAsync(int pageNumber = 1, int pageSize = 12);
    Task<Category?> GetByIdAsync(int id);
    Task<List<Category>> GetByCategoryNameAsync(string name);
    Task<List<Category>> GetSubCategoriesAsync(int parentCategoryId);
    Task<int> AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
