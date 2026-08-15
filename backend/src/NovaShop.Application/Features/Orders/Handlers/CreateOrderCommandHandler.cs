using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Orders.Commands;
using NovaShop.Application.Mappers;
using NovaShop.Application.Messages;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Orders.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");

        var order = new Order
        {
            UserId = request.UserId,
            TotalAmount = cart.TotalAmount
        };

        foreach (var item in cart.Items)
        {
            order.AddItem(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        var orderId = await _orderRepository.AddAsync(order);

        // Publish Event
        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = orderId,
            UserId = request.UserId,
            TotalAmount = order.TotalAmount
        });

        // Clear Cart
        await _cartRepository.ClearAsync(request.UserId);

        return orderId;
    }
}
