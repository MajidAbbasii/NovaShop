using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface ICartItemRepository
{
    Task<CartItem?> GetByIdAsync(int id);
    Task<List<CartItem>> GetByCartIdAsync(int cartId);
    Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId);
    Task<int> AddAsync(CartItem cartItem);
    Task UpdateAsync(CartItem cartItem);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
