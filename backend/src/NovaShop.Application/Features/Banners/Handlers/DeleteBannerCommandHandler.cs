using MediatR;
using NovaShop.Application.Features.Banners.Commands;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Banners.Handlers;

public class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public DeleteBannerCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteBannerCommand request, CancellationToken ct)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        if (banner == null) return false;

        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
