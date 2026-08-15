using MediatR;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Carts.Handlers;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, bool>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    public AddToCartCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return false;

        ProductColor? color = null;
        if (request.ProductColorId.HasValue)
        {
            color = product.Colors.FirstOrDefault(c => c.Id == request.ProductColorId.Value);
            if (color == null)
                return false; // color does not belong to this product
            if (!color.IsActive || color.Stock < request.Quantity)
                return false;
        }
        else if (product.Colors.Count > 0)
        {
            // Product has colors; a color must be selected. Reject ambiguous adds.
            return false;
        }

        if (product.Stock < request.Quantity)
            return false;

        var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
        if (cart == null)
        {
            cart = new Cart { UserId = request.UserId };
            await _cartRepository.AddAsync(cart);
        }

        cart.AddItem(product, request.Quantity, color?.Id, color?.Name ?? string.Empty, color?.Price);
        await _cartRepository.UpdateAsync(cart);

        return true;
    }
}
