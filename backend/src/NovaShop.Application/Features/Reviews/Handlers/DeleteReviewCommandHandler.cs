using MediatR;
using NovaShop.Application.Features.Reviews.Commands;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Reviews.Handlers;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public DeleteReviewCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _context.Reviews.FindAsync(new object[] { request.Id }, cancellationToken);
        if (review == null) return false;

        // Ownership guard: a user may only delete their own review (unless an admin/system
        // caller omits the user id). Prevents deleting other customers' reviews (IDOR).
        if (request.RequestingUserId != null && review.UserId != request.RequestingUserId.Value)
            throw new UnauthorizedAccessException("شما دسترسی به این دیدگاه ندارید");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}