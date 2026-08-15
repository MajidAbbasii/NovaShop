using MediatR;
using NovaShop.Application.Features.Products.Dtos;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Products.Handlers;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly ProductMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository repository, ProductMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        return product is null ? null : _mapper.ToDto(product);
    }
}
