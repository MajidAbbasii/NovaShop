using MediatR;
using NovaShop.Application.Features.Banners.Queries;
using NovaShop.Application.Features.Banners.Dtos;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Banners.Handlers;

public class GetActiveBannersHandler : IRequestHandler<GetActiveBannersQuery, List<BannerDto>>
{
    private readonly NovaShopDbContext _context;

    public GetActiveBannersHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<List<BannerDto>> Handle(GetActiveBannersQuery request, CancellationToken ct)
    {
        return await _context.Banners
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .Select(b => new BannerDto
            {
                Id = b.Id,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ImageUrl = b.ImageUrl,
                LinkUrl = b.LinkUrl,
                SortOrder = b.SortOrder
            })
            .ToListAsync(ct);
    }
}