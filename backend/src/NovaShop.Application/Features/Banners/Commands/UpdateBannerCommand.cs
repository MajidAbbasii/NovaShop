using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Banners.Commands;

public record UpdateBannerCommand(
    int Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? LinkUrl,
    bool IsActive,
    int SortOrder) : IRequest<bool>;

public class UpdateBannerCommandValidator : AbstractValidator<UpdateBannerCommand>
{
    public UpdateBannerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subtitle).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(2000);
        RuleFor(x => x.LinkUrl).MaximumLength(2000);
    }
}