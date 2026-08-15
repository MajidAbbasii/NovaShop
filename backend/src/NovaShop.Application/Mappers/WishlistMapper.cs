using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Wishlists.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class WishlistMapper
{
    [MapProperty(nameof(WishlistItem.Product) + "." + nameof(Product.Name), nameof(WishlistItemDto.ProductName))]
    [MapProperty(nameof(WishlistItem.Product) + "." + nameof(Product.Price), nameof(WishlistItemDto.ProductPrice))]
    [MapProperty(nameof(WishlistItem.Product) + "." + nameof(Product.ImageUrl), nameof(WishlistItemDto.ProductImageUrl))]
    public partial WishlistItemDto ToDto(WishlistItem item);

    public partial List<WishlistItemDto> ToDtoList(List<WishlistItem> items);
}
