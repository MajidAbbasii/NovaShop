using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Features.Products.Commands;

public record ProductImageInput(string Url, string? AltText, int DisplayOrder, bool IsPrimary, int? ProductColorId = null);
public record ProductColorInput(string Name, string? HexCode, int Stock, bool IsActive, decimal? Price = null);

public record CreateProductCommand : IRequest<int>
{
    public CreateProductCommand(string name, decimal price, string imageUrl, int stock, int categoryId)
    {
        Name = name;
        Price = price;
        ImageUrl = imageUrl;
        Stock = stock;
        CategoryId = categoryId;
    }

    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public decimal? OriginalPrice { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int Stock { get; init; }
    public int CategoryId { get; init; }
    public List<ProductImageInput> Images { get; init; } = new();
    public List<ProductColorInput> Colors { get; init; } = new();
}
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly NovaShopDbContext _db;

    public CreateProductCommandValidator(NovaShopDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام محصول اجباری است")
            .MinimumLength(3).WithMessage("نام محصول باید حداقل ۳ کاراکتر باشد");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("قیمت باید بیشتر از صفر باشد");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("موجودی نمی‌تواند منفی باشد");

        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("تصویر محصول اجباری است");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("دسته‌بندی محصول اجباری است")
            .MustAsync(BeExistingCategory)
            .WithMessage("دسته‌بندی انتخاب‌شده معتبر نیست");
    }

    private async Task<bool> BeExistingCategory(int categoryId, CancellationToken ct)
        => categoryId <= 0 || await _db.Categories.AnyAsync(c => c.Id == categoryId, ct);
}
