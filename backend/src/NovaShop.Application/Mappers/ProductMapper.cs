using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Application.Features.Products.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product product);
    public partial Product ToEntity(CreateProductCommand command);
    public partial void UpdateEntity(UpdateProductCommand command, Product product);

    public partial List<ProductDto> ToDtoList(List<Product> products);

    public ProductImageDto ToDto(ProductImage image) => new()
    {
        Id = image.Id,
        ProductColorId = image.ProductColorId,
        Url = image.Url,
        AltText = image.AltText,
        DisplayOrder = image.DisplayOrder,
        IsPrimary = image.IsPrimary
    };

    public ProductColorDto ToDto(ProductColor color) => new()
    {
        Id = color.Id,
        Name = color.Name,
        HexCode = color.HexCode,
        Stock = color.Stock,
        Price = color.Price,
        IsActive = color.IsActive,
        Images = color.Images.OrderBy(i => i.DisplayOrder).Select(ToDto).ToList()
    };
}
