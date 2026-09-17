using MediatR;
using NovaShop.Application.Features.Admin.Dtos;

namespace NovaShop.Application.Features.Admin.Queries;

public record AdminDashboardQuery() : IRequest<AdminDashboardDto>;
