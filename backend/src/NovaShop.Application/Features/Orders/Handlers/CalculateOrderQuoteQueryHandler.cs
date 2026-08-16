using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Orders.Dtos;
using NovaShop.Application.Features.Orders.Queries;
using NovaShop.Application.Services;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

public class CalculateOrderQuoteQueryHandler
    : IRequestHandler<CalculateOrderQuoteQuery, OrderQuoteDto>
{
    private readonly NovaShopDbContext _context;
    private readonly IShippingCostService _shippingCostService;
    private readonly IDiscountRepository _discountRepository;

    public CalculateOrderQuoteQueryHandler(
        NovaShopDbContext context,
        IShippingCostService shippingCostService,
        IDiscountRepository discountRepository)
    {
        _context = context;
        _shippingCostService = shippingCostService;
        _discountRepository = discountRepository;
    }

    public async Task<OrderQuoteDto> Handle(
        CalculateOrderQuoteQuery request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null || cart.Items.Count == 0)
            throw new InvalidOperationException("سبد خرید خالی است.");

        // Server-trusted subtotal from the cart (item unit prices come from DB).
        // Free-shipping threshold is evaluated on the PRE-discount subtotal
        // (matching the existing business rule).
        var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

        decimal discountAmount = 0m;
        string? discountCode = null;

        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            var discount = await _discountRepository
                .GetByCodeIgnoringCaseAsync(request.DiscountCode.Trim());
            if (discount != null && discount.IsValid(DateTime.UtcNow) && subtotal >= discount.MinOrderAmount)
            {
                discountAmount = discount.CalculateDiscount(subtotal);
                discountCode = discount.Code;
            }
        }

        var shipping = _shippingCostService.Calculate(subtotal, request.ShippingMethod);

        return new OrderQuoteDto
        {
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            DiscountCode = discountCode,
            ShippingCost = shipping.ShippingCost,
            IsFreeShipping = shipping.IsFreeShipping,
            ShippingMethod = shipping.ShippingMethod,
            GrandTotal = subtotal - discountAmount + shipping.ShippingCost
        };
    }
}
