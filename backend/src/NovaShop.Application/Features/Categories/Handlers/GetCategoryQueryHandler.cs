using MediatR;
using NovaShop.Application.Features.Categories.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;

namespace NovaShop.Application.Features.Categories.Handlers;

public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, NovaShop.Application.Features.Categories.Dtos.CategoryDto>
{
    private readonly NovaShopDbContext _context;
    private readonly CategoryMapper _mapper;

    public GetCategoryQueryHandler(NovaShopDbContext context, CategoryMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<NovaShop.Application.Features.Categories.Dtos.CategoryDto> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object[] { request.Id }, cancellationToken);
        return category == null ? new NovaShop.Application.Features.Categories.Dtos.CategoryDto() : _mapper.ToDto(category);
    }
}
