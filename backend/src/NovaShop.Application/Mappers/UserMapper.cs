using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Users.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User user);
    public partial List<UserDto> ToDtoList(List<User> users);
}
