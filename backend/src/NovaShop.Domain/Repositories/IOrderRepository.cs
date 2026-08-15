using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetAllAsync(int pageNumber = 1, int pageSize = 12);
    Task<Order?> GetByIdAsync(int id);
    Task<List<Order>> GetByUserIdAsync(int userId);
    Task<List<Order>> GetByStatusAsync(string status);
    Task<int> AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
