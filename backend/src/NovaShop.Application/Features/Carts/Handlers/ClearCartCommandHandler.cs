using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Carts.Handlers;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly ICartRepository _cartRepository;

    public ClearCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _cartRepository.ClearAsync(request.UserId);
        return true;
    }
}
