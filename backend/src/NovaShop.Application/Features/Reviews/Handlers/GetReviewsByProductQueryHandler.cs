using MediatR;
using NovaShop.Application.Features.Reviews.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Reviews.Handlers;

public class GetReviewsByProductQueryHandler : IRequestHandler<GetReviewsByProductQuery, List<NovaShop.Application.Features.Reviews.Dtos.ReviewDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly ReviewMapper _mapper;

    public GetReviewsByProductQueryHandler(NovaShopDbContext context, ReviewMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<NovaShop.Application.Features.Reviews.Dtos.ReviewDto>> Handle(GetReviewsByProductQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.Reviews.Where(r => r.ProductId == request.ProductId).ToListAsync(cancellationToken);
        return _mapper.ToDtoList(items);
    }
}
