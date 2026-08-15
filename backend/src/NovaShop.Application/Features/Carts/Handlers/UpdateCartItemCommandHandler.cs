using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Carts.Handlers;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, bool>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;

    public UpdateCartItemCommandHandler(ICartRepository cartRepository, ICartItemRepository cartItemRepository)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
    }

    public async Task<bool> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
        if (cart == null) return false;

        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
        if (item == null) return false;

        if (request.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        await _cartRepository.UpdateAsync(cart);
        return true;
    }
}
