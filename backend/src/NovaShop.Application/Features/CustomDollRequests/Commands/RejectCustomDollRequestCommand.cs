using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.CustomDollRequests.Commands;

public record RejectCustomDollRequestCommand(
    int RequestId,
    int AdminId,
    string? AdminMessage) : IRequest<bool>;

public class RejectCustomDollRequestCommandValidator : AbstractValidator<RejectCustomDollRequestCommand>
{
    public RejectCustomDollRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0);
        RuleFor(x => x.AdminId).GreaterThan(0);
    }
}
