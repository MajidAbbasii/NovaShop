using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IOrderItemRepository
{
    Task<PagedResult<OrderItem>> GetAllAsync(int pageNumber = 1, int pageSize = 12);
    Task<OrderItem?> GetByIdAsync(int id);
    Task<List<OrderItem>> GetByOrderIdAsync(int orderId);
    Task<int> AddAsync(OrderItem orderItem);
    Task<bool> ExistsAsync(int id);
}
