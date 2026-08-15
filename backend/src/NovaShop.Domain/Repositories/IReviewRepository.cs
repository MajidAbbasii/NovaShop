using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IReviewRepository
{
    Task<PagedResult<Review>> GetAllAsync(int pageNumber = 1, int pageSize = 12);
    Task<Review?> GetByIdAsync(int id);
    Task<List<Review>> GetByProductIdAsync(int productId);
    Task<List<Review>> GetByUserIdAsync(int userId);
    Task<int> AddAsync(Review review);
    Task<bool> ExistsAsync(int id);
}
