using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfCartItemRepository : ICartItemRepository
{
    private readonly NovaShopDbContext _context;

    public EfCartItemRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<CartItem?> GetByIdAsync(int id)
    {
        return await _context.CartItems
            .Include(ci => ci.Product)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == id);
    }

    public async Task<List<CartItem>> GetByCartIdAsync(int cartId)
    {
        return await _context.CartItems
            .Where(ci => ci.CartId == cartId)
            .Include(ci => ci.Product)
            .ToListAsync();
    }

    public async Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId)
    {
        return await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
    }

    public async Task<int> AddAsync(CartItem cartItem)
    {
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync();
        return cartItem.Id;
    }

    public async Task UpdateAsync(CartItem cartItem)
    {
        _context.CartItems.Update(cartItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cartItem = await GetByIdAsync(id);
        if (cartItem != null)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.CartItems.AnyAsync(ci => ci.Id == id);
    }
}
