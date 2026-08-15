using MediatR;
using NovaShop.Application.Features.Categories.Commands;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Categories.Handlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public DeleteCategoryCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null) return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
