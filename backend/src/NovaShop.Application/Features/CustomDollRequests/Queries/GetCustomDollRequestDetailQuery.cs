using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;

namespace NovaShop.Application.Features.CustomDollRequests.Queries;

public record GetCustomDollRequestDetailQuery(int RequestId) : IRequest<CustomDollRequestDetailResponse?>;