using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Carts.Handlers;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, bool>
{
    private readonly ICartRepository _cartRepository;

    public RemoveCartItemCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<bool> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
        if (cart == null) return false;

        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
        if (item == null) return false;

        cart.Items.Remove(item);
        await _cartRepository.UpdateAsync(cart);
        return true;
    }
}
