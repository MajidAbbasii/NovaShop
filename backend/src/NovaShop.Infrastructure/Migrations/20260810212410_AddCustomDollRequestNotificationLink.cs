using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDollRequestNotificationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomDollRequestId",
                table: "AppNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_CustomDollRequestId",
                table: "AppNotifications",
                column: "CustomDollRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppNotifications_CustomDollRequests_CustomDollRequestId",
                table: "AppNotifications",
                column: "CustomDollRequestId",
                principalTable: "CustomDollRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppNotifications_CustomDollRequests_CustomDollRequestId",
                table: "AppNotifications");

            migrationBuilder.DropIndex(
                name: "IX_AppNotifications_CustomDollRequestId",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "CustomDollRequestId",
                table: "AppNotifications");
        }
    }
}
