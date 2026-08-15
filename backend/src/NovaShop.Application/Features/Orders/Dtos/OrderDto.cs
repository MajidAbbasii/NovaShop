namespace NovaShop.Application.Features.Orders.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalTotal { get; set; }
    public string? DiscountCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string ShippingMethod { get; set; } = "POST";
    public decimal ShippingCost { get; set; }
    public string? PickupLocation { get; set; }
    public string? PickupInstructions { get; set; }
    public string? TrackingCode { get; set; }
    public string? TrackingNumber { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReadyForPickupAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentDto? Payment { get; set; }
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class OrderStatusHistoryDto
{
    public int Id { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ChangedByUserId { get; set; }
    public string ChangedByRole { get; set; } = "System";
    public DateTime ChangedAt { get; set; }
}

public class WalletDto
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "IRT";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<WalletTransactionDto> Transactions { get; set; } = new();
}

public class WalletTransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public int? OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AppNotificationDto
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public int? CustomDollRequestId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
