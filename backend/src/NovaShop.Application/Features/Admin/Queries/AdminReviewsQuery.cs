using MediatR;
using NovaShop.Application.Features.Admin.Dtos;
using NovaShop.Domain.Common;

namespace NovaShop.Application.Features.Admin.Queries;

public record AdminReviewsQuery(int? Rating, int PageNumber, int PageSize) : IRequest<PagedResult<AdminReviewDto>>;
