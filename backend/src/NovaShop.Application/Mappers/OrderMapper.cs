using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class OrderMapper
{
    public OrderDto ToDto(Order order)
    {
        if (order == null) return new OrderDto();

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            OriginalTotal = order.OriginalTotal,
            DiscountCode = order.DiscountCode,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            ShippingMethod = order.ShippingMethod,
            ShippingCost = order.ShippingCost,
            PickupLocation = order.PickupLocation,
            PickupInstructions = order.PickupInstructions,
            TrackingCode = order.TrackingCode,
            TrackingNumber = order.TrackingNumber,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            ReadyForPickupAt = order.ReadyForPickupAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            RefundedAt = order.RefundedAt,
            RefundReason = order.RefundReason,
            RefundAmount = order.RefundAmount,
            Items = order.Items.Select(ToDto).ToList(),
            Payment = order.Payment == null ? null : ToDto(order.Payment),
            StatusHistory = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(ToDto)
                .ToList()
        };
    }

    public OrderItemDto ToDto(OrderItem item)
    {
        return new OrderItemDto
        {
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            ProductColorId = item.ProductColorId,
            ColorName = item.ColorName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            ImageUrl = item.Product?.ImageUrl
        };
    }

    public PaymentDto ToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            CreatedAt = payment.CreatedAt
        };
    }

    public OrderStatusHistoryDto ToDto(OrderStatusHistory h)
    {
        return new OrderStatusHistoryDto
        {
            Id = h.Id,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            Note = h.Note,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByRole = h.ChangedByRole,
            ChangedAt = h.ChangedAt
        };
    }

    public InventoryTransactionDto ToDto(InventoryTransaction t)
    {
        return new InventoryTransactionDto
        {
            Id = t.Id,
            ProductId = t.ProductId,
            ProductName = t.Product?.Name ?? string.Empty,
            OrderId = t.OrderId,
            Type = t.Type,
            Quantity = t.Quantity,
            StockBefore = t.StockBefore,
            StockAfter = t.StockAfter,
            Reference = t.Reference,
            CreatedAt = t.CreatedAt
        };
    }

    public SmsNotificationDto ToDto(SmsNotification n)
    {
        return new SmsNotificationDto
        {
            Id = n.Id,
            OrderId = n.OrderId,
            PhoneNumber = n.PhoneNumber,
            EventType = n.EventType,
            Message = n.Message,
            Provider = n.Provider,
            Status = n.Status,
            ProviderMessageId = n.ProviderMessageId,
            Error = n.Error,
            CreatedAt = n.CreatedAt,
            SentAt = n.SentAt
        };
    }

    public List<OrderDto> ToDtoList(List<Order> orders)
    {
        return orders?.Select(ToDto).ToList() ?? new List<OrderDto>();
    }

    public List<OrderItemDto> ToDtoList(List<OrderItem> items)
    {
        return items?.Select(ToDto).ToList() ?? new List<OrderItemDto>();
    }

    public List<PaymentDto> ToDtoList(List<Payment> payments)
    {
        return payments?.Select(ToDto).ToList() ?? new List<PaymentDto>();
    }
}
