using MediatR;
using NovaShop.Application.Features.Banners.Commands;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Banners.Handlers;

public class UpdateBannerCommandHandler : IRequestHandler<UpdateBannerCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public UpdateBannerCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateBannerCommand request, CancellationToken ct)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        if (banner == null) return false;

        banner.Title = request.Title.Trim();
        banner.Subtitle = (request.Subtitle ?? string.Empty).Trim();
        banner.ImageUrl = (request.ImageUrl ?? string.Empty).Trim();
        banner.LinkUrl = (request.LinkUrl ?? string.Empty).Trim();
        banner.IsActive = request.IsActive;
        banner.SortOrder = request.SortOrder;
        banner.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}
