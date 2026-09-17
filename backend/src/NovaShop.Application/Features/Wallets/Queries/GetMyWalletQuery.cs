using MediatR;
using NovaShop.Application.Features.Wallets.Dtos;

namespace NovaShop.Application.Features.Wallets.Queries;

public record GetMyWalletQuery(int UserId, int PageSize = 50) : IRequest<WalletDto>;
