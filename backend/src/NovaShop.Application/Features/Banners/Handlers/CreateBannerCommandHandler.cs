using MediatR;
using NovaShop.Application.Features.Banners.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Banners.Handlers;

public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, int>
{
    private readonly NovaShopDbContext _context;

    public CreateBannerCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateBannerCommand request, CancellationToken ct)
    {
        var banner = new Banner
        {
            Title = request.Title.Trim(),
            Subtitle = (request.Subtitle ?? string.Empty).Trim(),
            ImageUrl = (request.ImageUrl ?? string.Empty).Trim(),
            LinkUrl = (request.LinkUrl ?? string.Empty).Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        _context.Banners.Add(banner);
        await _context.SaveChangesAsync(ct);
        return banner.Id;
    }
}
