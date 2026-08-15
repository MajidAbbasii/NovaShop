using NovaShop.Domain.Entities;

namespace NovaShop.Common.Extensions;

public static class ProductExtensions
{
    // Extension Block
    extension(Product product)
    {
        // Extension Property
        public decimal DiscountPercentage => product.OriginalPrice.HasValue
            ? Math.Round(((product.OriginalPrice.Value - product.Price) / product.OriginalPrice.Value) * 100, 1)
            : 0m;

        // Extension Method
        public string GetDisplayInfo()
        {
            var discount = product.DiscountPercentage;
            return discount > 0
                ? $"{product.Name} - {product.Price:C} (تخفیف {discount}%)"
                : $"{product.Name} - {product.Price:C}";
        }

        // Extension Operator (جدید)
        public static Product operator +(Product p1, int stockAdd)
        {
            p1.Stock += stockAdd;
            return p1;
        }
    }
}
