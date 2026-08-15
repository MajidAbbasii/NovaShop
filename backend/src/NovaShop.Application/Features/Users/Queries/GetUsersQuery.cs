using MediatR;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Users.Dtos;

namespace NovaShop.Application.Features.Users.Queries;

public record GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public string? Role { get; init; }
}

public record GetUserQuery(int Id) : IRequest<UserDto>;
