using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class RemoveDiscountFromOrderCommandHandler : IRequestHandler<Commands.RemoveDiscountFromOrderCommand, OrderDto>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _orderMapper;
    private readonly ILogger<RemoveDiscountFromOrderCommandHandler> _logger;

    public RemoveDiscountFromOrderCommandHandler(
        NovaShopDbContext context,
        OrderMapper orderMapper,
        ILogger<RemoveDiscountFromOrderCommandHandler> logger)
    {
        _context = context;
        _orderMapper = orderMapper;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(Commands.RemoveDiscountFromOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException("سفارش یافت نشد");

        if (order.UserId != request.UserId)
            throw new InvalidOperationException("این سفارش متعلق به شما نیست");

        if (!order.DiscountId.HasValue)
            throw new InvalidOperationException("هیچ تخفیفی روی این سفارش اعمال نشده است");

        // Decrement the discount usage count back
        if (order.DiscountId.HasValue)
        {
            var discount = await _context.Discounts.FindAsync(new object[] { order.DiscountId.Value }, cancellationToken);
            if (discount != null && discount.UsedCount > 0)
            {
                discount.UsedCount--;
            }
        }

        order.RemoveDiscount();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Discount removed from order {OrderId}", request.OrderId);

        return _orderMapper.ToDto(order);
    }
}
