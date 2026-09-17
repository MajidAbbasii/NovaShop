using MediatR;
using NovaShop.Application.Features.CustomDollRequests.Dtos;

namespace NovaShop.Application.Features.CustomDollRequests.Queries;

public record GetMyCustomDollRequestDetailQuery(int RequestId, int UserId) : IRequest<CustomDollRequestDetailResponse?>;