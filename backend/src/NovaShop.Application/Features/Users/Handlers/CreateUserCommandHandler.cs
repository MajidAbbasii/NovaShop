using MediatR;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Users.Handlers;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(NovaShopDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = string.IsNullOrWhiteSpace(request.Password)
                ? string.Empty
                : _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            PhoneNumber = request.PhoneNumber ?? string.Empty
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
