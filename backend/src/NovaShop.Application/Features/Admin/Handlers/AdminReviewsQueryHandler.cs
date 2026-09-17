using MediatR;
using NovaShop.Application.Features.Admin.Queries;
using NovaShop.Application.Features.Admin.Dtos;
using NovaShop.Domain.Common;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Admin.Handlers;

public class AdminReviewsQueryHandler : IRequestHandler<AdminReviewsQuery, PagedResult<AdminReviewDto>>
{
    private readonly NovaShopDbContext _context;

    public AdminReviewsQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminReviewDto>> Handle(AdminReviewsQuery request, CancellationToken ct)
    {
        var query = _context.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .AsNoTracking()
            .AsQueryable();

        if (request.Rating is >= 1 and <= 5)
            query = query.Where(r => r.Rating == request.Rating.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                UserId = r.UserId,
                UserName = r.User.Username,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<AdminReviewDto>(items, totalCount, request.PageNumber, request.PageSize, totalPages);
    }
}