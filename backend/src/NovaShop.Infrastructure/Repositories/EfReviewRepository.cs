using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfReviewRepository : IReviewRepository
{
    private readonly NovaShopDbContext _context;

    public EfReviewRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Review>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Reviews.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Include(r => r.Product)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Review>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId)
    {
        return await _context.Reviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetByUserIdAsync(int userId)
    {
        return await _context.Reviews
            .Where(r => r.UserId == userId)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review.Id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Reviews.AnyAsync(r => r.Id == id);
    }
}
