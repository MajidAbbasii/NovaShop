using MediatR;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Users.Handlers;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserCommandHandler(NovaShopDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user == null) return false;

        if (request.Username != null) user.Username = request.Username;
        if (request.Email != null) user.Email = request.Email;
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.Role != null) user.Role = request.Role;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (!string.IsNullOrWhiteSpace(request.Password)) user.PasswordHash = _passwordHasher.Hash(request.Password);

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
