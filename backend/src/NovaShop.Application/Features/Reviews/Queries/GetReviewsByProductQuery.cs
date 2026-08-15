using MediatR;
using NovaShop.Application.Features.Reviews.Dtos;

namespace NovaShop.Application.Features.Reviews.Queries;

public record GetReviewsByProductQuery(int ProductId) : IRequest<List<ReviewDto>>;
