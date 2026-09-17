using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Notifications.Commands;

public record MarkNotificationReadCommand(int NotificationId, int UserId) : IRequest<bool>;

public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
