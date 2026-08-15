using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Currency standardization: migrate all monetary columns to Iranian Toman.
    /// Legacy rows stored a USD-style decimal scale (e.g. 34.99); rescale ×10000
    /// into native Toman (34.99 → 349,900) so existing carts/orders/discounts
    /// keep their relative value. New installs seed Toman values directly.
    /// </summary>
    public partial class CurrencyStandardizeToToman : Migration
    {
        private static readonly (string Table, string Column)[] MoneyColumns =
        {
            ("Products", "Price"),
            ("Products", "OriginalPrice"),
            ("OrderItems", "UnitPrice"),
            ("Orders", "TotalAmount"),
            ("Orders", "DiscountAmount"),
            ("Orders", "OriginalTotal"),
            ("Payments", "Amount"),
            ("Discounts", "Value"),
            ("Discounts", "MinOrderAmount"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column) in MoneyColumns)
            {
                migrationBuilder.Sql(
                    $"UPDATE [{table}] SET [{column}] = [{column}] * 10000 WHERE [{column}] IS NOT NULL");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, column) in MoneyColumns)
            {
                migrationBuilder.Sql(
                    $"UPDATE [{table}] SET [{column}] = [{column}] / 10000 WHERE [{column}] IS NOT NULL");
            }
        }
    }
}
