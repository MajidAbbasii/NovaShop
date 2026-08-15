using MediatR;
using NovaShop.Application.Features.Wishlists.Queries;
using NovaShop.Application.Features.Wishlists.Dtos;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Common;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Wishlists.Handlers;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, PagedResult<WishlistItemDto>>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly WishlistMapper _mapper;

    public GetWishlistQueryHandler(IWishlistRepository wishlistRepository, WishlistMapper mapper)
    {
        _wishlistRepository = wishlistRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<WishlistItemDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var result = await _wishlistRepository.GetByUserIdAsync(request.UserId, request.PageNumber, request.PageSize);
        var items = _mapper.ToDtoList(result.Items);
        return new PagedResult<WishlistItemDto>(items, result.TotalCount, result.PageNumber, result.PageSize, result.TotalPages);
    }
}
