using Riok.Mapperly.Abstractions;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Reviews.Dtos;

namespace NovaShop.Application.Mappers;

[Mapper]
public partial class ReviewMapper
{
    public partial ReviewDto ToDto(Review review);
    public partial List<ReviewDto> ToDtoList(List<Review> reviews);
}
