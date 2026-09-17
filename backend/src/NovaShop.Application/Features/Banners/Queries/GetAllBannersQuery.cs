using MediatR;
using NovaShop.Application.Features.Banners.Dtos;

namespace NovaShop.Application.Features.Banners.Queries;

public record GetAllBannersQuery() : IRequest<List<BannerDto>>;