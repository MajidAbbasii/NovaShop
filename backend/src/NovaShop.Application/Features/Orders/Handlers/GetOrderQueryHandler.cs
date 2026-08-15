using MediatR;
using NovaShop.Application.Features.Orders.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Orders.Handlers;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, NovaShop.Application.Features.Orders.Dtos.OrderDto>
{
    private readonly NovaShopDbContext _context;
    private readonly OrderMapper _mapper;

    public GetOrderQueryHandler(NovaShopDbContext context, OrderMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<NovaShop.Application.Features.Orders.Dtos.OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        return order == null ? new NovaShop.Application.Features.Orders.Dtos.OrderDto() : _mapper.ToDto(order);
    }
}
