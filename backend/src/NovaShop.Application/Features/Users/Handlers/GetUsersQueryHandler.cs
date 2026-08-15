using MediatR;
using NovaShop.Application.Features.Users.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace NovaShop.Application.Features.Users.Handlers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<NovaShop.Application.Features.Users.Dtos.UserDto>>
{
    private readonly NovaShopDbContext _context;
    private readonly UserMapper _mapper;

    public GetUsersQueryHandler(NovaShopDbContext context, UserMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<NovaShop.Application.Features.Users.Dtos.UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(u => u.Role == request.Role);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtoList = _mapper.ToDtoList(items);
        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        return new PagedResult<NovaShop.Application.Features.Users.Dtos.UserDto>(dtoList, total, request.PageNumber, request.PageSize, totalPages);
    }
}
