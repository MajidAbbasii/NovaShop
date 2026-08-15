using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Reviews.Dtos;

namespace NovaShop.Application.Features.Reviews.Queries;

public record GetReviewsByProductQuery(int ProductId) : IRequest<List<ReviewDto>>;
