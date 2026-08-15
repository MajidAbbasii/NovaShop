using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfWishlistRepository : IWishlistRepository
{
    private readonly NovaShopDbContext _context;

    public EfWishlistRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WishlistItem item)
    {
        _context.Set<WishlistItem>().Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveAsync(int userId, int productId)
    {
        var item = await _context.Set<WishlistItem>()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        if (item == null) return false;

        _context.Set<WishlistItem>().Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<WishlistItem>> GetByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Set<WishlistItem>()
            .Include(w => w.Product)
            .Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(w => w.AddedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WishlistItem>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<bool> ExistsAsync(int userId, int productId)
    {
        return await _context.Set<WishlistItem>().AnyAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.Set<WishlistItem>().CountAsync(w => w.UserId == userId);
    }
}
