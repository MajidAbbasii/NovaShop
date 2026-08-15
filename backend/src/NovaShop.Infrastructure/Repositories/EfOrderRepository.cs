using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly NovaShopDbContext _context;

    public EfOrderRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Order>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Orders.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .ToListAsync();
    }

    public async Task<List<Order>> GetByStatusAsync(string status)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .Include(o => o.User)
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order.Id;
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }
}
