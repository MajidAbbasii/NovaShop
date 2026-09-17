using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;

namespace NovaShop.Application.Features.CustomDollRequests.Queries;

public record GetMyCustomDollRequestsQuery(int UserId, int PageNumber = 1, int PageSize = 50) : IRequest<CustomDollRequestListResponse>;