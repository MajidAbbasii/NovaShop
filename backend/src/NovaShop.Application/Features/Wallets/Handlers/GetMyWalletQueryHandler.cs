using MediatR;
using NovaShop.Application.Features.Wallets.Dtos;
using NovaShop.Application.Features.Wallets.Queries;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Wallets.Handlers
{
    public class GetMyWalletQueryHandler : IRequestHandler<GetMyWalletQuery, WalletDto>
    {
        private readonly NovaShopDbContext _context;

        public GetMyWalletQueryHandler(NovaShopDbContext context)
        {
            _context = context;
        }

        public async Task<WalletDto> Handle(GetMyWalletQuery request, CancellationToken ct)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, ct);

            if (wallet == null)
            {
                wallet = new NovaShop.Domain.Entities.Wallet { UserId = request.UserId, Balance = 0m };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync(ct);
            }

            return new WalletDto
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt,
                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(request.PageSize)
                    .Select(t => new WalletTransactionDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        BalanceBefore = t.BalanceBefore,
                        BalanceAfter = t.BalanceAfter,
                        Type = t.Type,
                        Description = t.Description,
                        Reference = t.Reference,
                        OrderId = t.OrderId,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}
