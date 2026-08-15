using MediatR;
using NovaShop.Application.Features.Reviews.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Reviews.Handlers;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, int>
{
    private readonly NovaShopDbContext _context;

    public CreateReviewCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = request.UserId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
