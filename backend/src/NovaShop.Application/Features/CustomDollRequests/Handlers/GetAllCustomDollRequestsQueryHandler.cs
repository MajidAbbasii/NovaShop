using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Application.Features.CustomDollRequests.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class GetAllCustomDollRequestsQueryHandler : IRequestHandler<GetAllCustomDollRequestsQuery, CustomDollRequestListResponse>
{
    private readonly NovaShopDbContext _context;

    public GetAllCustomDollRequestsQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<CustomDollRequestListResponse> Handle(GetAllCustomDollRequestsQuery request, CancellationToken ct)
    {
        var query = _context.CustomDollRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminCustomDollRequestDto
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
            .ToListAsync(ct);

        return new CustomDollRequestListResponse
        {
            Items = items.Cast<CustomDollRequestDto>().ToList(),
            Total = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}