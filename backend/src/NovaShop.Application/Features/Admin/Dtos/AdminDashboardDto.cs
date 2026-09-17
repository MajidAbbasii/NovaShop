using System.Text.Json.Serialization;

namespace NovaShop.Application.Features.Admin.Dtos;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal Revenue { get; set; }
    public List<DailyRevenuePoint> DailyRevenue { get; set; } = new();
    public List<RecentOrderSummary> RecentOrders { get; set; } = new();
}

public class DailyRevenuePoint
{
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }
    [JsonPropertyName("Revenue")]
    public decimal Revenue { get; set; }
}

public class RecentOrderSummary
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
