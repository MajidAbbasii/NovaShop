using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Carts.Handlers;

public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, int>
{
    private readonly ICartRepository _cartRepository;

    public CreateCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<int> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var cart = new Cart { UserId = request.UserId };
        var id = await _cartRepository.AddAsync(cart);
        return id;
    }
}
