using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IWishlistRepository
{
    Task AddAsync(WishlistItem item);
    Task<bool> RemoveAsync(int userId, int productId);
    Task<PagedResult<WishlistItem>> GetByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 12);
    Task<bool> ExistsAsync(int userId, int productId);
    Task<int> CountByUserIdAsync(int userId);
}
