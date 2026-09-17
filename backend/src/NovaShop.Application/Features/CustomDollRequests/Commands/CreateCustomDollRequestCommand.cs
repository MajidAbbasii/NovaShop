using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.CustomDollRequests.Commands;

public record CreateCustomDollRequestCommand(
    int UserId,
    string ImageUrl,
    string Title,
    string? Description,
    string BodyColor,
    string EyeColor,
    int Height) : IRequest<int>;

public class CreateCustomDollRequestCommandValidator : AbstractValidator<CreateCustomDollRequestCommand>
{
    public CreateCustomDollRequestCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyColor).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EyeColor).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Height).GreaterThan(0);
    }
}