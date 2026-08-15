using MediatR;
using NovaShop.Application.Features.Products.Dtos;

namespace NovaShop.Application.Features.Products.Queries;

public record GetProductByIdQuery : IRequest<ProductDto?>
{
    public int Id { get; init; }
}
