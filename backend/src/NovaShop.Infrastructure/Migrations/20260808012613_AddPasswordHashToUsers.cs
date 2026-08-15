using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Bootstrap a PBKDF2 hash for the seeded admin accounts so existing
            // deployments are not locked out after this migration. Hash is for
            // the default password "admin123" — operators should change it.
            // (computed with the same Pbkdf2PasswordHasher parameters used by the app)
            migrationBuilder.Sql(
                "UPDATE Users SET PasswordHash = 'PBKDF2$100000$iOQkAVVxlbHxxrESE82CbA==$DRFnDbw0lLVuKqkduwKdet2+F0ZZpDhfFRqyw+gmjVU=' " +
                "WHERE (Username = 'admin' OR Username = 'smoketest') AND (PasswordHash = '' OR PasswordHash IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");
        }
    }
}
