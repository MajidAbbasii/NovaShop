using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Categories.Commands;

public record CreateCategoryCommand(string Name) : IRequest<int>
{
    public string Name { get; init; } = Name;
    public string? Description { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
    }
}
