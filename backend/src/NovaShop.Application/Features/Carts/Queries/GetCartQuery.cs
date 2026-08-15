using MediatR;
using NovaShop.Application.Features.Carts.Dtos;

namespace NovaShop.Application.Features.Carts.Queries;

public record GetCartQuery(int UserId) : IRequest<CartDto>;
