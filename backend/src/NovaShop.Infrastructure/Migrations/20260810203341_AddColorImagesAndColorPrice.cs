using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColorImagesAndColorPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductColorId",
                table: "ProductImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ProductColors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductColorId",
                table: "ProductImages",
                column: "ProductColorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_ProductColors_ProductColorId",
                table: "ProductImages",
                column: "ProductColorId",
                principalTable: "ProductColors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_ProductColors_ProductColorId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductColorId",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ProductColorId",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ProductColors");
        }
    }
}
