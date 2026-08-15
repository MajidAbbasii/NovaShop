using MediatR;
using NovaShop.Application.Features.Carts.Queries;
using NovaShop.Domain.Repositories;
using NovaShop.Application.Features.Carts.Dtos;
using NovaShop.Application.Mappers;

namespace NovaShop.Application.Features.Carts.Handlers;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly CartMapper _mapper;   // اگر Mapper داری

    public GetCartQueryHandler(ICartRepository cartRepository, CartMapper mapper)
    {
        _cartRepository = cartRepository;
        _mapper = mapper;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
        return cart == null ? new CartDto() : _mapper.ToDto(cart);
    }
}
