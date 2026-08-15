using MediatR;
using NovaShop.Application.Features.Orders.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Common;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Handlers;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<NovaShop.Application.Features.Orders.Dtos.OrderDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _mapper;

    public GetOrdersQueryHandler(NovaShopDbContext context, OrderMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<NovaShop.Application.Features.Orders.Dtos.OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.StatusHistory)
            .AsNoTracking()
            .AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(o => o.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(o =>
                o.Id.ToString().Contains(term) ||
                (o.TrackingCode != null && o.TrackingCode.Contains(term)) ||
                (o.TrackingNumber != null && o.TrackingNumber.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtoList = _mapper.ToDtoList(items);
        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResult<NovaShop.Application.Features.Orders.Dtos.OrderDto>(dtoList, total, request.PageNumber, request.PageSize, totalPages);
    }
}

public class GetInventoryTransactionsQueryHandler : IRequestHandler<GetInventoryTransactionsQuery, PagedResult<InventoryTransactionDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _mapper;

    public GetInventoryTransactionsQueryHandler(NovaShopDbContext context, OrderMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<InventoryTransactionDto>> Handle(GetInventoryTransactionsQuery request, CancellationToken ct)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Product)
            .AsNoTracking()
            .AsQueryable();

        if (request.ProductId.HasValue)
            query = query.Where(t => t.ProductId == request.ProductId.Value);
        if (request.OrderId.HasValue)
            query = query.Where(t => t.OrderId == request.OrderId.Value);
        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(t => t.Type == request.Type);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResult<InventoryTransactionDto>(
            items.Select(t => _mapper.ToDto(t)).ToList(),
            total, request.PageNumber, request.PageSize, totalPages);
    }
}

public class GetSmsNotificationsQueryHandler : IRequestHandler<GetSmsNotificationsQuery, PagedResult<SmsNotificationDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _mapper;

    public GetSmsNotificationsQueryHandler(NovaShopDbContext context, OrderMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SmsNotificationDto>> Handle(GetSmsNotificationsQuery request, CancellationToken ct)
    {
        var query = _context.SmsNotifications.AsNoTracking().AsQueryable();

        if (request.OrderId.HasValue)
            query = query.Where(n => n.OrderId == request.OrderId.Value);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(n => n.Status == request.Status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResult<SmsNotificationDto>(
            items.Select(n => _mapper.ToDto(n)).ToList(),
            total, request.PageNumber, request.PageSize, totalPages);
    }
}
