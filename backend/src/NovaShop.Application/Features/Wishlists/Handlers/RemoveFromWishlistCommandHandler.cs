using MediatR;
using NovaShop.Application.Features.Wishlists.Commands;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Wishlists.Handlers;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, bool>
{
    private readonly IWishlistRepository _wishlistRepository;

    public RemoveFromWishlistCommandHandler(IWishlistRepository wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        return await _wishlistRepository.RemoveAsync(request.UserId, request.ProductId);
    }
}
