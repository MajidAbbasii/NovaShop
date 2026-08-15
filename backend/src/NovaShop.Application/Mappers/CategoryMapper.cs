using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Categories.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class CategoryMapper
{
    public partial CategoryDto ToDto(Category category);
    public partial List<CategoryDto> ToDtoList(List<Category> categories);
}
