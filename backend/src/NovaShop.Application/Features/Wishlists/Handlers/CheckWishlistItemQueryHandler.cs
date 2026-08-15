using MediatR;
using NovaShop.Application.Features.Wishlists.Queries;
using NovaShop.Application.Features.Wishlists.Dtos;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Wishlists.Handlers;

public class CheckWishlistItemQueryHandler : IRequestHandler<CheckWishlistItemQuery, WishlistCheckResponse>
{
    private readonly IWishlistRepository _wishlistRepository;

    public CheckWishlistItemQueryHandler(IWishlistRepository wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<WishlistCheckResponse> Handle(CheckWishlistItemQuery request, CancellationToken cancellationToken)
    {
        var exists = await _wishlistRepository.ExistsAsync(request.UserId, request.ProductId);
        return new WishlistCheckResponse { Exists = exists };
    }
}
