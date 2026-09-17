using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand(int UserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandValidator : AbstractValidator<MarkAllNotificationsReadCommand>
{
    public MarkAllNotificationsReadCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
