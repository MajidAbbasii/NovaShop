using MediatR;
using NovaShop.Application.Features.Banners.Dtos;

namespace NovaShop.Application.Features.Banners.Queries;

public record GetActiveBannersQuery() : IRequest<List<BannerDto>>;