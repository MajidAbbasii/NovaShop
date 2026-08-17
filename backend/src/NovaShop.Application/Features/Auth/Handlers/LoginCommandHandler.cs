using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Application.Services;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly NovaShopDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public LoginCommandHandler(NovaShopDbContext context, IPasswordHasher passwordHasher, IJwtTokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Empty credentials fail fast (same message as any other failure).
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            throw new UnauthorizedAccessException("Invalid username or password");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid username or password");

        // Password verification is mandatory — a token is issued ONLY after a
        // successful hash comparison.
        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

        return await _tokenService.GenerateAndPersistAsync(user, cancellationToken);
    }
}
