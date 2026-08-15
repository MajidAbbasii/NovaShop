using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IDiscountRepository
{
    Task<Discount?> GetByCodeAsync(string code);

    Task<Discount?> GetByCodeIgnoringCaseAsync(string code);
    Task<Discount?> GetByIdAsync(int id);
    Task<PagedResult<Discount>> GetAllAsync(int pageNumber = 1, int pageSize = 12);
    Task<int> AddAsync(Discount discount);
    Task UpdateAsync(Discount discount);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> SaveChangesAsync();
}