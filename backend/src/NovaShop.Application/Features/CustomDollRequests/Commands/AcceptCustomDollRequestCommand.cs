using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.CustomDollRequests.Commands;

public record AcceptCustomDollRequestCommand(int RequestId, int UserId) : IRequest<bool>;

public class AcceptCustomDollRequestCommandValidator : AbstractValidator<AcceptCustomDollRequestCommand>
{
    public AcceptCustomDollRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
