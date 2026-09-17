using MediatR;
using NovaShop.Application.Features.Admin.Queries;
using NovaShop.Application.Features.Admin.Dtos;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Admin.Handlers;

public class AdminDashboardQueryHandler : IRequestHandler<AdminDashboardQuery, AdminDashboardDto>
{
    private readonly NovaShopDbContext _context;

    public AdminDashboardQueryHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> Handle(AdminDashboardQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var totalUsersTask = _context.Users.CountAsync(ct);
        var totalOrdersTask = _context.Orders.CountAsync(ct);
        var pendingOrdersTask = _context.Orders.CountAsync(o => o.Status == "Pending", ct);
        var revenueTask = _context.Orders
            .Where(o => o.Status == "Delivered" || o.Status == "Shipped")
            .SumAsync(o => o.TotalAmount, ct);

        var dailyRevenueTask = _context.Orders
            .Where(o => o.CreatedAt >= now.AddDays(-7) && (o.Status == "Delivered" || o.Status == "Shipped"))
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailyRevenuePoint { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        var recentOrdersTask = _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderSummary
            {
                Id = o.Id,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        await Task.WhenAll(totalUsersTask, totalOrdersTask, pendingOrdersTask, revenueTask, dailyRevenueTask, recentOrdersTask);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsersTask.Result,
            TotalOrders = totalOrdersTask.Result,
            PendingOrders = pendingOrdersTask.Result,
            Revenue = revenueTask.Result,
            DailyRevenue = dailyRevenueTask.Result,
            RecentOrders = recentOrdersTask.Result
        };
    }
}