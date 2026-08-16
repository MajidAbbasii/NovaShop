using MediatR;
using NovaShop.Application.Features.Orders.Dtos;

namespace NovaShop.Application.Features.Orders.Queries;

public record CalculateOrderQuoteQuery(
    int UserId,
    string ShippingMethod,
    string? DiscountCode = null
) : IRequest<OrderQuoteDto>;
