using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Categories.Commands;

public record UpdateCategoryCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).MinimumLength(2).When(x => x.Name != null);
    }
}
