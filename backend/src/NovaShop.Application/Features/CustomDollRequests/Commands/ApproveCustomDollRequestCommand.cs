using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.CustomDollRequests.Commands;

public record ApproveCustomDollRequestCommand(
    int RequestId,
    int AdminId,
    decimal Price,
    string? AdminMessage) : IRequest<bool>;

public class ApproveCustomDollRequestCommandValidator : AbstractValidator<ApproveCustomDollRequestCommand>
{
    public ApproveCustomDollRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0);
        RuleFor(x => x.AdminId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
