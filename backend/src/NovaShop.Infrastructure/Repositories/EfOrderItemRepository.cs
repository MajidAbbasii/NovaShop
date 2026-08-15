using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfOrderItemRepository : IOrderItemRepository
{
    private readonly NovaShopDbContext _context;

    public EfOrderItemRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<OrderItem>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.OrderItems.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderItem>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<OrderItem?> GetByIdAsync(int id)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == id);
    }

    public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
    {
        return await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Include(oi => oi.Product)
            .ToListAsync();
    }

    public async Task<int> AddAsync(OrderItem orderItem)
    {
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();
        return orderItem.Id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.OrderItems.AnyAsync(oi => oi.Id == id);
    }
}
