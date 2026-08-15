using MediatR;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Users.Handlers;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public DeleteUserCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
