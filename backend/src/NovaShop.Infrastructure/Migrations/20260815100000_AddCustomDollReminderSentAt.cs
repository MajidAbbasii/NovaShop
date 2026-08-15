using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDollReminderSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime?>(
                name: "ReminderSentAt",
                table: "CustomDollRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "CustomDollRequests");
        }
    }
}
