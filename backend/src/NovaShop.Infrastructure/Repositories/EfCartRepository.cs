using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfCartRepository : ICartRepository
{
    private readonly NovaShopDbContext _context;

    public EfCartRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByIdAsync(int id)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<int> AddAsync(Cart cart)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart.Id;
    }

    public async Task UpdateAsync(Cart cart)
    {
        _context.Carts.Update(cart);
        await _context.SaveChangesAsync();
    }

    public async Task ClearAsync(int userId)
    {
        var cart = await GetByUserIdAsync(userId);
        if (cart != null)
        {
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var cart = await GetByIdAsync(id);
        if (cart != null)
        {
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Carts.AnyAsync(c => c.Id == id);
    }
}
