namespace NovaShop.Application.Features.ShippingSettings;

public record ShippingSettingsDto(
    decimal CourierPrice,
    decimal PostPrice,
    decimal PostFreeShippingThreshold,
    decimal PickupPrice
);

public record ShippingMethodDto(
    string Method,
    string DisplayKey,
    decimal Price,
    bool IsFree
);

public record ShippingMethodsDto(
    IReadOnlyList<ShippingMethodDto> Methods,
    decimal PostFreeShippingThreshold
);
