using MediatR;
using NovaShop.Application.Features.Users.Queries;
using NovaShop.Infrastructure.Data;
using NovaShop.Application.Mappers;

namespace NovaShop.Application.Features.Users.Handlers;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, NovaShop.Application.Features.Users.Dtos.UserDto>
{
    private readonly NovaShopDbContext _context;
    private readonly UserMapper _mapper;

    public GetUserQueryHandler(NovaShopDbContext context, UserMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<NovaShop.Application.Features.Users.Dtos.UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        return user == null ? new NovaShop.Application.Features.Users.Dtos.UserDto() : _mapper.ToDto(user);
    }
}
