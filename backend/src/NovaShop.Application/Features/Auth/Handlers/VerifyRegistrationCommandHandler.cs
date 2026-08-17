using MediatR;
using NovaShop.Application.Features.Auth.Commands;
using NovaShop.Application.Services;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Auth.Handlers;

public class VerifyRegistrationCommandHandler : IRequestHandler<VerifyRegistrationCommand, LoginResponse>
{
    private readonly NovaShopDbContext _context;
    private readonly OtpStore _otpStore;
    private readonly PendingRegistrationStore _pendingStore;
    private readonly IJwtTokenService _tokenService;

    public VerifyRegistrationCommandHandler(NovaShopDbContext context, OtpStore otpStore, PendingRegistrationStore pendingStore, IJwtTokenService tokenService)
    {
        _context = context;
        _otpStore = otpStore;
        _pendingStore = pendingStore;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(VerifyRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (!_otpStore.TryVerify(request.PhoneNumber, request.Code))
            throw new UnauthorizedAccessException("کد وارد شده نامعتبر یا منقضی شده است");

        if (!_pendingStore.TryTake(request.PhoneNumber, out var username, out var passwordHash))
            throw new UnauthorizedAccessException("ابتدا فرم ثبت‌نام را تکمیل کنید");

        var user = new User
        {
            Username = username,
            Email = $"{request.PhoneNumber}@novashop.local",
            PasswordHash = passwordHash,
            Role = User.RoleCustomer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FirstName = "",
            LastName = "",
            PhoneNumber = request.PhoneNumber,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return await _tokenService.GenerateAndPersistAsync(user, cancellationToken);
    }
}
