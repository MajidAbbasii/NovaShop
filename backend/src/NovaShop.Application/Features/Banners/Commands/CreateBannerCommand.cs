using FluentValidation;
using MediatR;

namespace NovaShop.Application.Features.Banners.Commands;

public record CreateBannerCommand(
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? LinkUrl,
    bool IsActive = true,
    int SortOrder = 0) : IRequest<int>;

public class CreateBannerCommandValidator : AbstractValidator<CreateBannerCommand>
{
    public CreateBannerCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subtitle).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(2000);
        RuleFor(x => x.LinkUrl).MaximumLength(2000);
    }
}