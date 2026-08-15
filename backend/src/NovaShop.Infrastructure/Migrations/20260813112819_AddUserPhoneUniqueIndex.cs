using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhoneUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Backfill empty and duplicate values so the UNIQUE indexes below don't collide.
            // Keep the lowest-Id row of each colliding group intact; suffix the rest with their Id.
            migrationBuilder.Sql(@"
UPDATE u SET PhoneNumber = 'phone_' + CAST(u.Id AS NVARCHAR(20))
FROM Users u
WHERE EXISTS (
    SELECT 1 FROM Users d
    WHERE d.PhoneNumber = u.PhoneNumber
      AND d.Id < u.Id
      AND u.PhoneNumber IS NOT NULL AND u.PhoneNumber <> ''
);
UPDATE u SET Username = 'user_' + CAST(u.Id AS NVARCHAR(50))
FROM Users u
WHERE EXISTS (
    SELECT 1 FROM Users d
    WHERE d.Username = u.Username
      AND d.Id < u.Id
      AND u.Username IS NOT NULL AND u.Username <> ''
);
UPDATE Users SET PhoneNumber = 'phone_' + CAST(Id AS NVARCHAR(20)) WHERE PhoneNumber IS NULL OR PhoneNumber = '';
UPDATE Users SET Username = 'user_' + CAST(Id AS NVARCHAR(50)) WHERE Username IS NULL OR Username = '';
");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
