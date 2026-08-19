using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropShippingSettingsIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot ALTER the IDENTITY property in place, so drop the
            // PK, drop the Id column entirely, recreate it as a plain int PK (no
            // IDENTITY), then re-add the PK. ShippingSettings is a single-row
            // singleton seeded lazily, so there is no data to preserve.
            migrationBuilder.DropPrimaryKey(
                name: "PK_ShippingSettings",
                table: "ShippingSettings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ShippingSettings");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ShippingSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShippingSettings",
                table: "ShippingSettings",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ShippingSettings",
                table: "ShippingSettings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ShippingSettings");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ShippingSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShippingSettings",
                table: "ShippingSettings",
                column: "Id");
        }
    }
}
