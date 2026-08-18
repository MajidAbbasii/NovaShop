using FluentValidation;
using MediatR;
using NovaShop.Application.Features.ShippingSettings;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.ShippingSettings.Commands;

public record UpdateShippingSettingsCommand(
    decimal CourierPrice,
    decimal PostPrice,
    decimal PostFreeShippingThreshold,
    decimal PickupPrice
) : IRequest<ShippingSettingsDto>;

public class UpdateShippingSettingsCommandValidator : AbstractValidator<UpdateShippingSettingsCommand>
{
    public UpdateShippingSettingsCommandValidator()
    {
        RuleFor(x => x.CourierPrice)
            .GreaterThanOrEqualTo(0).WithMessage("هزینه پیک نمی‌تواند منفی باشد");
        RuleFor(x => x.PostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("هزینه پست نمی‌تواند منفی باشد");
        RuleFor(x => x.PickupPrice)
            .GreaterThanOrEqualTo(0).WithMessage("هزینه تحویل حضوری نمی‌تواند منفی باشد");
        RuleFor(x => x.PostFreeShippingThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("حد آستانه ارسال رایگان نمی‌تواند منفی باشد");
    }
}

public class UpdateShippingSettingsCommandHandler
    : IRequestHandler<UpdateShippingSettingsCommand, ShippingSettingsDto>
{
    private readonly IShippingSettingsRepository _settings;

    public UpdateShippingSettingsCommandHandler(IShippingSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<ShippingSettingsDto> Handle(UpdateShippingSettingsCommand request, CancellationToken ct)
    {
        var entity = new ShippingSetting
        {
            CourierPrice = Math.Round(request.CourierPrice, 2),
            PostPrice = Math.Round(request.PostPrice, 2),
            PostFreeShippingThreshold = Math.Round(request.PostFreeShippingThreshold, 2),
            PickupPrice = Math.Round(request.PickupPrice, 2),
        };

        await _settings.UpdateAsync(entity, ct);

        return new ShippingSettingsDto(
            entity.CourierPrice, entity.PostPrice, entity.PostFreeShippingThreshold, entity.PickupPrice);
    }
}
