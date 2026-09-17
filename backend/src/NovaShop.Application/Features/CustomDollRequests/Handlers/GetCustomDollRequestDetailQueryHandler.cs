using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Features.CustomDollRequests.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class GetCustomDollRequestDetailQueryHandler : IRequestHandler<GetCustomDollRequestDetailQuery, CustomDollRequestDetailResponse?>
{
    private readonly NovaShopDbContext _context;

    public GetCustomDollRequestDetailQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<CustomDollRequestDetailResponse?> Handle(GetCustomDollRequestDetailQuery request, CancellationToken ct)
    {
        return await _context.CustomDollRequests
            .Where(r => r.Id == request.RequestId)
            .Select(r => new CustomDollRequestDetailResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                CustomerUsername = r.User.Username,
                CustomerPhone = r.User.PhoneNumber,
                CustomerEmail = r.User.Email,
                ImageUrl = r.ImageUrl,
                Title = r.Title,
                Description = r.Description,
                BodyColor = r.BodyColor,
                EyeColor = r.EyeColor,
                Height = r.Height,
                Status = r.Status,
                Price = r.Price,
                Currency = r.Currency,
                AdminMessage = r.AdminMessage,
                ReviewedBy = r.ReviewedBy,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ReviewedAt = r.ReviewedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}
