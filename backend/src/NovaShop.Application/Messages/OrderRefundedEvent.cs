namespace NovaShop.Application.Messages;

public class OrderRefundedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; }
}
