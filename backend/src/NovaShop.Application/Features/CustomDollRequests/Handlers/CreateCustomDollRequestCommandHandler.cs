using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Commands;
using NovaShop.Application.Features.CustomDollRequests.Dtos;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.CustomDollRequests.Handlers;

public class CreateCustomDollRequestCommandHandler : IRequestHandler<CreateCustomDollRequestCommand, int>
{
    private readonly NovaShopDbContext _context;

    public CreateCustomDollRequestCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCustomDollRequestCommand request, CancellationToken ct)
    {
        var entity = new CustomDollRequest
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            Description = (request.Description ?? string.Empty).Trim(),
            BodyColor = request.BodyColor.Trim(),
            EyeColor = request.EyeColor.Trim(),
            Height = request.Height,
            Status = CustomDollRequest.StatusPendingReview,
            Currency = CustomDollRequest.CurrencyToman
        };

        _context.CustomDollRequests.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }
}