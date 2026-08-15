using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create full-text catalog
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'NovaShopCatalog')
                    CREATE FULLTEXT CATALOG NovaShopCatalog AS DEFAULT;
                """,
                suppressTransaction: true);

            // Create full-text index on Products(Name, Description)
            // Uses the auto-generated PK_Products as the unique index
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT * FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('Products'))
                BEGIN
                    CREATE FULLTEXT INDEX ON Products(
                        Name LANGUAGE 1033,
                        Description LANGUAGE 1033
                    )
                    KEY INDEX PK_Products
                    ON NovaShopCatalog
                    WITH (CHANGE_TRACKING AUTO);
                END
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT * FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID('Products'))
                    DROP FULLTEXT INDEX ON Products;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'NovaShopCatalog')
                    DROP FULLTEXT CATALOG NovaShopCatalog;
                """,
                suppressTransaction: true);
        }
    }
}
