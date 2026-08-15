using MediatR;

namespace NovaShop.Application.Features.Discounts.Commands;

public record DeleteDiscountCommand(int Id) : IRequest<bool>;
