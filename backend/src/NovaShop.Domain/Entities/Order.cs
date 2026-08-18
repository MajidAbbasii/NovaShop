namespace NovaShop.Domain.Entities;

public class Order
{
    // Status constants
    public const string StatusPending = "Pending";
    public const string StatusConfirmed = "Confirmed";
    public const string StatusProcessing = "Processing";
    public const string StatusPaid = "Paid";
    public const string StatusReadyForPickup = "ReadyForPickup";
    public const string StatusShipped = "Shipped";
    public const string StatusDelivered = "Delivered";
    public const string StatusCancelled = "Cancelled";
    public const string StatusRefunded = "Refunded";
    public const string StatusFailed = "Failed";
    public const string StatusReturnRequested = "ReturnRequested";
    public const string StatusReturnApproved = "ReturnApproved";
    public const string StatusReturned = "Returned";

    // Shipping methods
    public const string ShippingPost = "POST";
    public const string ShippingCourier = "COURIER";
    public const string ShippingPickup = "PICKUP";

    // Payment methods
    public const string PaymentMethodCashOnDelivery = "CashOnDelivery";
    public const string PaymentMethodInPerson = "InPerson";
    public const string PaymentMethodCod = "COD";

    // Payment status constants
    public const string PaymentPending = "Pending";
    public const string PaymentPaid = "Paid";
    public const string PaymentFailed = "Failed";
    public const string PaymentRefunded = "Refunded";
    public const string PaymentExpired = "Expired";

    /// <summary>Allowed status transitions. Empty array = terminal.</summary>
    public static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        [StatusPending] = [StatusConfirmed, StatusPaid, StatusCancelled, StatusFailed],
        [StatusConfirmed] = [StatusProcessing, StatusPaid, StatusCancelled, StatusFailed],
        [StatusProcessing] = [StatusPaid, StatusReadyForPickup, StatusShipped, StatusCancelled, StatusFailed],
        [StatusPaid] = [StatusProcessing, StatusShipped, StatusReadyForPickup, StatusCancelled, StatusRefunded],
        [StatusReadyForPickup] = [StatusDelivered, StatusCancelled],
        [StatusShipped] = [StatusDelivered, StatusCancelled],
        [StatusDelivered] = [StatusReturnRequested],
        [StatusReturnRequested] = [StatusReturnApproved, StatusDelivered],
        [StatusReturnApproved] = [StatusReturned, StatusReturnRequested],
        [StatusReturned] = [StatusRefunded],
        [StatusRefunded] = [],
        [StatusCancelled] = [StatusRefunded],
        [StatusFailed] = [],
    };

    public static bool IsValidTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var next) && next.Contains(to);

    public int Id { get; set; }
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = StatusPending;

    // Shipping
    public string ShippingMethod { get; set; } = ShippingPost;
    public decimal ShippingCost { get; set; }
    public string? PickupLocation { get; set; }
    public string? PickupInstructions { get; set; }

    // Payment status (mirrors Payment entity; lives on Order for fast filtering)
    public string PaymentStatus { get; set; } = PaymentPending;
    public string? PaymentMethod { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;

    // Tracking
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string? TrackingCode { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string? TrackingNumber { get; set; }

    // Idempotency
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? ReservationExpiresAt { get; set; }

    // Discount
    public int? DiscountId { get; set; }
    public Discount? Discount { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalTotal { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReadyForPickupAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public bool RefundProcessed { get; set; }

    public List<OrderItem> Items { get; private set; } = new();
    public Payment? Payment { get; private set; }
    public List<OrderStatusHistory> StatusHistory { get; private set; } = new();

    // Domain Methods
    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        OriginalTotal = Items.Sum(i => i.Quantity * i.UnitPrice);
        TotalAmount = OriginalTotal - DiscountAmount;
    }

    public void ApplyDiscount(Discount discount, decimal orderTotal)
    {
        if (DiscountId.HasValue)
            throw new InvalidOperationException("Discount already applied to this order");

        if (!discount.IsValid(DateTime.UtcNow))
            throw new InvalidOperationException("Discount is not valid");

        if (orderTotal < discount.MinOrderAmount)
            throw new InvalidOperationException("Order total below minimum amount for this discount");

        OriginalTotal = orderTotal;
        DiscountId = discount.Id;
        DiscountCode = discount.Code;
        DiscountAmount = discount.CalculateDiscount(orderTotal);
        TotalAmount = OriginalTotal - DiscountAmount;

        discount.IncrementUsage();
    }

    public void RemoveDiscount()
    {
        DiscountId = null;
        Discount = null;
        DiscountCode = null;
        DiscountAmount = 0m;
        TotalAmount = OriginalTotal;
    }

    public void MarkAsPaid()
    {
        TransitionTo(StatusPaid);
        PaidAt = DateTime.UtcNow;
        PaymentStatus = PaymentPaid;
    }

    public void MarkAsReadyForPickup()
    {
        TransitionTo(StatusReadyForPickup);
        ReadyForPickupAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        TransitionTo(StatusDelivered);
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded(decimal amount, string? reason = null)
    {
        if (RefundProcessed)
            throw new InvalidOperationException("این سفارش قبلاً بازگشت وجه شده است");

        TransitionTo(StatusRefunded);
        RefundedAt = DateTime.UtcNow;
        RefundAmount = amount;
        RefundReason = reason;
        RefundProcessed = true;
        PaymentStatus = PaymentRefunded;
    }

    public bool CanRefund => PaymentStatus == PaymentPaid && !RefundProcessed;

    public void MarkAsShipped(string? trackingCode = null, string? trackingNumber = null)
    {
        TransitionTo(StatusShipped);
        ShippedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(trackingCode))
            TrackingCode = trackingCode;
        if (!string.IsNullOrWhiteSpace(trackingNumber))
            TrackingNumber = trackingNumber;
    }

    // Checkout flow domain methods
    public void Confirm()
    {
        TransitionTo(StatusConfirmed);
    }

    public void MarkAsProcessing()
    {
        TransitionTo(StatusProcessing);
    }

    public void Cancel()
    {
        if (Status is StatusCancelled or StatusDelivered or StatusRefunded)
            throw new InvalidOperationException($"سفارش با وضعیت {Status} قابل لغو نیست");
        TransitionTo(StatusCancelled);
        CancelledAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        TransitionTo(StatusFailed);
    }

    public bool CanBePaid => Status is StatusPending or StatusConfirmed or StatusProcessing;

    /// <summary>Transition with validation; records history entry.</summary>
    public OrderStatusHistory TransitionTo(string newStatus, string? note = null, int changedByUserId = 0, string changedByRole = "System")
    {
        if (Status == newStatus)
            throw new InvalidOperationException($"سفارش در وضعیت {Status} است");

        if (!IsValidTransition(Status, newStatus))
            throw new InvalidOperationException($"انتقال از وضعیت '{Status}' به '{newStatus}' مجاز نیست");

        var history = new OrderStatusHistory
        {
            OrderId = Id,
            FromStatus = Status,
            ToStatus = newStatus,
            Note = note,
            ChangedByUserId = changedByUserId,
            ChangedByRole = changedByRole,
            ChangedAt = DateTime.UtcNow
        };

        StatusHistory.Add(history);
        Status = newStatus;
        return history;
    }

    /// <summary>Generate a human-friendly tracking code like NS-2026-000123.</summary>
    public void AssignTrackingCode()
    {
        TrackingCode ??= $"NS-{CreatedAt:yyyy}-{Id:D6}";
    }
}
