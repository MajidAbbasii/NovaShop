using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Products.Commands;

public record UpdateProductCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public string? ImageUrl { get; init; }
    public int? Stock { get; init; }
    public int? CategoryId { get; init; }
    public List<ProductImageInput>? Images { get; init; }
    public List<ProductColorInput>? Colors { get; init; }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly NovaShopDbContext _db;

    public UpdateProductCommandValidator(NovaShopDbContext db)
    {
        _db = db;

        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).MinimumLength(3).When(x => x.Name != null);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => x.Stock.HasValue);
        RuleFor(x => x.CategoryId)
            .MustAsync(BeExistingCategory)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("دسته‌بندی انتخاب‌شده معتبر نیست");
    }

    private async Task<bool> BeExistingCategory(int? categoryId, CancellationToken ct)
        => !categoryId.HasValue || await _db.Categories.AnyAsync(c => c.Id == categoryId.Value, ct);
}
