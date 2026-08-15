using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Reviews.Commands;

public record CreateReviewCommand : IRequest<int>
{
    public int ProductId { get; init; }
    public int UserId { get; set; }
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
}

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1,5);
    }
}
