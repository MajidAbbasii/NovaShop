using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Wishlists.Dtos;

namespace NovaShop.Application.Features.Wishlists.Queries;

public record GetWishlistQuery(int UserId, int PageNumber = 1, int PageSize = 12) : IRequest<PagedResult<WishlistItemDto>>;
