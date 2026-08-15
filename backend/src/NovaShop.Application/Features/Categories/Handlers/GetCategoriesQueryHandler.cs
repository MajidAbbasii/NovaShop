using MediatR;
using NovaShop.Application.Features.Categories.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;
using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;

namespace NovaShop.Application.Features.Categories.Handlers;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, PagedResult< NovaShop.Application.Features.Categories.Dtos.CategoryDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly CategoryMapper _mapper;

    public GetCategoriesQueryHandler(NovaShopDbContext context, CategoryMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult< NovaShop.Application.Features.Categories.Dtos.CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories.AsQueryable();
        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(c => c.Name.Contains(request.SearchTerm));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        var dtoList = _mapper.ToDtoList(items);
        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResult< NovaShop.Application.Features.Categories.Dtos.CategoryDto>(dtoList, total, request.PageNumber, request.PageSize, totalPages);
    }
}
