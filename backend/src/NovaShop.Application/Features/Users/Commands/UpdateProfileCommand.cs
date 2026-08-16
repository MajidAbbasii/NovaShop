using FluentValidation;
using MediatR;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NovaShop.Application.Features.Users.Commands;

/// <summary>
/// Customer self-service profile update. The user id is taken from the JWT, never
/// from the request body, so a customer can only edit their own record. Sensitive
/// fields (Role, IsActive, PasswordHash, Id) are intentionally NOT part of this command.
/// </summary>
public record UpdateProfileCommand : IRequest<bool>
{
    public int UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Valid email is required");
        RuleFor(x => x.PhoneNumber)
            .Matches("^09\\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Valid phone number is required (e.g. 09123456789)");
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public UpdateProfileCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) return false;

        // Enforce phone uniqueness without blocking the user's own existing number.
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
        {
            var taken = await _context.Users.AnyAsync(
                u => u.PhoneNumber == request.PhoneNumber && u.Id != request.UserId, cancellationToken);
            if (taken) throw new InvalidOperationException("این شماره موبایل قبلاً استفاده شده است");
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Email != null) user.Email = request.Email;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.Address != null) user.Address = request.Address;
        if (request.City != null) user.City = request.City;
        if (request.PostalCode != null) user.PostalCode = request.PostalCode;

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
