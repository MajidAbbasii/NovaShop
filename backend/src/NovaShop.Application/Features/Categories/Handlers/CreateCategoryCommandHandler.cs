using MediatR;
using NovaShop.Application.Features.Categories.Commands;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;

namespace NovaShop.Application.Features.Categories.Handlers;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
{
    private readonly NovaShopDbContext _context;
    private readonly CategoryMapper _mapper;

    public CreateCategoryCommandHandler(NovaShopDbContext context, CategoryMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            ImageUrl = request.ImageUrl
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
