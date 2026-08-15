using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryCartRepository : ICartRepository
{
    private readonly List<Cart> _carts = new();

    public async Task<Cart?> GetByIdAsync(int id)
    {
        return _carts.FirstOrDefault(c => c.Id == id);
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        return _carts.FirstOrDefault(c => c.UserId == userId);
    }

    public async Task<int> AddAsync(Cart cart)
    {
        cart.Id = _carts.Max(c => (int?)c.Id) + 1 ?? 1;
        _carts.Add(cart);
        return cart.Id;
    }

    public async Task UpdateAsync(Cart cart)
    {
        var existing = _carts.FirstOrDefault(c => c.Id == cart.Id);
        if (existing != null)
        {
            // Cart properties are mostly init-only, so we just keep it as is
        }
    }

    public async Task ClearAsync(int userId)
    {
        var cart = _carts.FirstOrDefault(c => c.UserId == userId);
        if (cart != null)
            _carts.Remove(cart);
    }

    public async Task DeleteAsync(int id)
    {
        var cart = _carts.FirstOrDefault(c => c.Id == id);
        if (cart != null)
            _carts.Remove(cart);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _carts.Any(c => c.Id == id);
    }
}
