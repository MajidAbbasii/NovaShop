using MediatR;
using NovaShop.Application.Features.Banners.Queries;
using NovaShop.Application.Features.Banners.Dtos;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Banners.Handlers;

public class GetAllBannersHandler : IRequestHandler<GetAllBannersQuery, List<BannerDto>>
{
    private readonly NovaShopDbContext _context;

    public GetAllBannersHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<List<BannerDto>> Handle(GetAllBannersQuery request, CancellationToken ct)
    {
        return await _context.Banners
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .Select(b => new BannerDto
            {
                Id = b.Id,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ImageUrl = b.ImageUrl,
                LinkUrl = b.LinkUrl,
                IsActive = b.IsActive,
                SortOrder = b.SortOrder,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync(ct);
    }
}