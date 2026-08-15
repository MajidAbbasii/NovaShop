using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryCartItemRepository : ICartItemRepository
{
    private readonly List<CartItem> _cartItems = new();

    public async Task<CartItem?> GetByIdAsync(int id)
    {
        return _cartItems.FirstOrDefault(ci => ci.Id == id);
    }

    public async Task<List<CartItem>> GetByCartIdAsync(int cartId)
    {
        return _cartItems.Where(ci => ci.CartId == cartId).ToList();
    }

    public async Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId)
    {
        return _cartItems.FirstOrDefault(ci => ci.CartId == cartId && ci.ProductId == productId);
    }

    public async Task<int> AddAsync(CartItem cartItem)
    {
        cartItem.Id = _cartItems.Max(ci => (int?)ci.Id) + 1 ?? 1;
        _cartItems.Add(cartItem);
        return cartItem.Id;
    }

    public async Task UpdateAsync(CartItem cartItem)
    {
        var existing = _cartItems.FirstOrDefault(ci => ci.Id == cartItem.Id);
        if (existing != null)
        {
            existing.Quantity = cartItem.Quantity;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var cartItem = _cartItems.FirstOrDefault(ci => ci.Id == id);
        if (cartItem != null)
            _cartItems.Remove(cartItem);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _cartItems.Any(ci => ci.Id == id);
    }
}
