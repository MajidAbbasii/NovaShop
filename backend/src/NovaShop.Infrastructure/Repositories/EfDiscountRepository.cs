using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfDiscountRepository : IDiscountRepository
{
    private readonly NovaShopDbContext _context;

    public EfDiscountRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<Discount?> GetByCodeAsync(string code)
    {
        return await _context.Discounts.FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<Discount?> GetByCodeIgnoringCaseAsync(string code)
    {
        return await _context.Discounts.FirstOrDefaultAsync(d => d.Code.ToUpper() == code.ToUpper());
    }

    public async Task<Discount?> GetByIdAsync(int id)
    {
        return await _context.Discounts.FindAsync(id);
    }

    public async Task<PagedResult<Discount>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Discounts.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Discount>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<int> AddAsync(Discount discount)
    {
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();
        return discount.Id;
    }

    public async Task UpdateAsync(Discount discount)
    {
        _context.Discounts.Update(discount);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var discount = await GetByIdAsync(id);
        if (discount != null)
        {
            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Discounts.AnyAsync(d => d.Id == id);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}