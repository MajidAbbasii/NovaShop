using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class ApplyDiscountToOrderCommandHandler : IRequestHandler<Commands.ApplyDiscountToOrderCommand, OrderDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IDiscountRepository _discountRepository;
    private readonly OrderMapper _orderMapper;
    private readonly ILogger<ApplyDiscountToOrderCommandHandler> _logger;

    public ApplyDiscountToOrderCommandHandler(
        NovaShopDbContext context,
        IDiscountRepository discountRepository,
        OrderMapper orderMapper,
        ILogger<ApplyDiscountToOrderCommandHandler> logger)
    {
        _context = context;
        _discountRepository = discountRepository;
        _orderMapper = orderMapper;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(Commands.ApplyDiscountToOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException("سفارش یافت نشد");

        if (order.UserId != request.UserId)
            throw new InvalidOperationException("این سفارش متعلق به شما نیست");

        if (order.Status != Domain.Entities.Order.StatusPending)
            throw new InvalidOperationException("تخفیف فقط برای سفارش‌های در انتظار قابل اعمال است");

        var discount = await _discountRepository.GetByCodeIgnoringCaseAsync(request.DiscountCode);
        if (discount == null)
            throw new InvalidOperationException("کد تخفیف معتبر نیست");

        if (!discount.IsValid(DateTime.UtcNow))
            throw new InvalidOperationException("کد تخفیف منقضی شده یا غیرفعال است");

        if (discount.UsedCount >= discount.UsageLimit)
            throw new InvalidOperationException("محدودیت استفاده از این کد تخفیف به پایان رسیده است");

        // Validate min order amount against the original total (without existing discounts)
        var orderTotal = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        if (orderTotal < discount.MinOrderAmount)
            throw new InvalidOperationException(
                $"حداقل مبلغ سفارش برای این تخفیف {discount.MinOrderAmount:N0} تومان است");

        // Product/category specific discount validation
        if (discount.ApplicableProductIds.Any() || discount.ApplicableCategoryIds.Any())
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var categoryIds = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => p.CategoryId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var anyApplicableProduct = order.Items.Any(i =>
                discount.ApplicableProductIds.Contains(i.ProductId));

            var anyApplicableCategory = discount.ApplicableCategoryIds.Any(catId =>
                categoryIds.Contains(catId));

            if (!anyApplicableProduct && !anyApplicableCategory)
                throw new InvalidOperationException("این تخفیف برای محصولات موجود در سفارش شما قابل استفاده نیست");
        }

        order.ApplyDiscount(discount, orderTotal);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Discount {DiscountCode} applied to order {OrderId}, discount amount: {Amount}",
            request.DiscountCode, request.OrderId, order.DiscountAmount);

        return _orderMapper.ToDto(order);
    }
}
