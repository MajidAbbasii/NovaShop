using NovaShop.Application.Features.Carts.Dtos;
using NovaShop.Domain.Entities;
using Riok.Mapperly.Abstractions;
using System.Linq;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class CartMapper
{
    public CartDto ToDto(Cart cart)
    {
        if (cart == null) return new CartDto();

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            TotalAmount = cart.TotalAmount,
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ImageUrl = i.Product?.ImageUrl ?? string.Empty,
                ProductColorId = i.ProductColorId,
                ColorName = i.ColorName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    public List<CartItemDto> ToDtoList(List<CartItem> items)
    {
        return items?.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            ImageUrl = i.Product?.ImageUrl ?? string.Empty,
            ProductColorId = i.ProductColorId,
            ColorName = i.ColorName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList() ?? new List<CartItemDto>();
    }
}
