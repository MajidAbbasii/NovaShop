using MediatR;
using NovaShop.Application.Features.Categories.Commands;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Categories.Handlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, bool>
{
    private readonly NovaShopDbContext _context;

    public UpdateCategoryCommandHandler(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        if (category == null) return false;

        if (request.Name != null) category.Name = request.Name;
        if (request.Description != null) category.Description = request.Description;
        if (request.ImageUrl != null) category.ImageUrl = request.ImageUrl;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
