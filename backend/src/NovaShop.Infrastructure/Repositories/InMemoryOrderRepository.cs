using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = new();

    public async Task<PagedResult<Order>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _orders.AsQueryable();

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Order>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    public async Task<List<Order>> GetByUserIdAsync(int userId)
    {
        return _orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<List<Order>> GetByStatusAsync(string status)
    {
        return _orders.Where(o => o.Status == status).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<int> AddAsync(Order order)
    {
        order.Id = _orders.Max(o => (int?)o.Id) + 1 ?? 1;
        _orders.Add(order);
        return order.Id;
    }

    public async Task UpdateAsync(Order order)
    {
        var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
        if (existing != null)
        {
            existing.Status = order.Status;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order != null)
            _orders.Remove(order);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _orders.Any(o => o.Id == id);
    }
}
