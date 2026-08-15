using MediatR;
using NovaShop.Application.Features.Wishlists.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Wishlists.Handlers;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, bool>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository;

    public AddToWishlistCommandHandler(IWishlistRepository wishlistRepository, IProductRepository productRepository)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null) return false;

        var exists = await _wishlistRepository.ExistsAsync(request.UserId, request.ProductId);
        if (exists) return true; // no-op, already in wishlist — idempotent

        var item = new WishlistItem
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            Note = request.Note
        };

        await _wishlistRepository.AddAsync(item);
        return true;
    }
}
