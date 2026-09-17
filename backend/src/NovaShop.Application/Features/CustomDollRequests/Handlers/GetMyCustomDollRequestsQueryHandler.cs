using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Features.CustomDollRequests.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class GetMyCustomDollRequestsQueryHandler : IRequestHandler<GetMyCustomDollRequestsQuery, CustomDollRequestListResponse>
{
    private readonly NovaShopDbContext _context;

    public GetMyCustomDollRequestsQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<CustomDollRequestListResponse> Handle(GetMyCustomDollRequestsQuery request, CancellationToken ct)
    {
        var query = _context.CustomDollRequests
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new CustomDollRequestDto
            {
                Id = r.Id,
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                Description = r.Description,
                BodyColor = r.BodyColor,
                EyeColor = r.EyeColor,
                Height = r.Height,
                Status = r.Status,
                Price = r.Price,
                Currency = r.Currency,
                AdminMessage = r.AdminMessage,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ReviewedAt = r.ReviewedAt
            })
            .ToListAsync(ct);

        return new CustomDollRequestListResponse
        {
            Items = items,
            Total = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
