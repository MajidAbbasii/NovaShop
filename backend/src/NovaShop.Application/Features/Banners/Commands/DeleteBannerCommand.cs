using MediatR;

namespace NovaShop.Application.Features.Banners.Commands;

public record DeleteBannerCommand(int Id) : IRequest<bool>;