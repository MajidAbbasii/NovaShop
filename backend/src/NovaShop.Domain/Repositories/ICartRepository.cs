using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(int id);
    Task<Cart?> GetByUserIdAsync(int userId);
    Task<int> AddAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task ClearAsync(int userId);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
