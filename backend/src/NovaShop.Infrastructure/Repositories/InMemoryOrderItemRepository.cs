using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryOrderItemRepository : IOrderItemRepository
{
    private readonly List<OrderItem> _orderItems = new();

    public async Task<PagedResult<OrderItem>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _orderItems.AsQueryable();

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .OrderByDescending(oi => oi.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<OrderItem>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<OrderItem?> GetByIdAsync(int id)
    {
        return _orderItems.FirstOrDefault(oi => oi.Id == id);
    }

    public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
    {
        return _orderItems.Where(oi => oi.OrderId == orderId).ToList();
    }

    public async Task<int> AddAsync(OrderItem orderItem)
    {
        orderItem.Id = _orderItems.Max(oi => (int?)oi.Id) + 1 ?? 1;
        _orderItems.Add(orderItem);
        return orderItem.Id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _orderItems.Any(oi => oi.Id == id);
    }
}
