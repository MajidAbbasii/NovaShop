namespace NovaShop.Application.Features.Orders.Dtos;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SmsNotificationDto
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
