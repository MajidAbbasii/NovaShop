using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryReviewRepository : IReviewRepository
{
    private readonly List<Review> _reviews = new();

    public async Task<PagedResult<Review>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _reviews.AsQueryable();

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Review>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return _reviews.FirstOrDefault(r => r.Id == id);
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId)
    {
        return _reviews.Where(r => r.ProductId == productId).OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<List<Review>> GetByUserIdAsync(int userId)
    {
        return _reviews.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<int> AddAsync(Review review)
    {
        review.Id = _reviews.Max(r => (int?)r.Id) + 1 ?? 1;
        _reviews.Add(review);
        return review.Id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _reviews.Any(r => r.Id == id);
    }
}
