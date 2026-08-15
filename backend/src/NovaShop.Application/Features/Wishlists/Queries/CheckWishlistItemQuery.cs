using MediatR;
using NovaShop.Application.Features.Wishlists.Dtos;

namespace NovaShop.Application.Features.Wishlists.Queries;

public record CheckWishlistItemQuery(int UserId, int ProductId) : IRequest<WishlistCheckResponse>;
