using MediatR;

namespace NovaShop.Application.Features.Reviews.Commands;

public record DeleteReviewCommand(int Id, int? RequestingUserId = null) : IRequest<bool>;