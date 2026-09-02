# PostgreSQL Migration Plan

## Current State (after analysis)

### Provider status
- EF Core: Source code already calls `UseNpgsql` (NovaShopServiceCollectionExtensions:235).
- Migrations: Only PostgreSQL migration set exists (`20260826061443_InitialCreatePostgres`). Good.
- Hangfire: Calls `UsePostgreSQLStorage` (line 141) BUT the `Hangfire.PostgreSQL` NuGet package is NOT installed. Only `Hangfire` (meta) + `Hangfire.SqlServer` are referenced.

### SQL Server remnants (must remove)
1. `.csproj` package references: `Microsoft.EntityFrameworkCore.SqlServer`, `Hangfire.SqlServer`, `AspNetCore.HealthChecks.SqlServer`, `Microsoft.Data.SqlClient`.
2. `appsettings.json` / `appsettings.Development.json`: localdb connection strings.
3. `docker-compose.yml`: SQL Server container + connection strings.
4. `docker-compose.prod.yml`: SQL Server container + connection strings.
5. Tests: `IntegrationWebApplicationFactory.cs` + `NovaShopIntegrationTestFixture.cs` use `Testcontainers.MsSql` + `UseSqlServer`.
6. `tools/NovaShop.Etl/Program.cs`: SQL Server source.
7. `GetProductSuggestionsQueryHandler.cs`: `TOP` + `CONTAINSTABLE` (SQL Server FTS).
8. `ProgramHelpers.cs:46`: malformed duplicate expression on health check.
9. `NovShop.Infrastructure.csproj`: still references `Microsoft.EntityFrameworkCore.SqlServer`.

### FTS / RebuildFtsCatalogJob
- `RebuildFtsCatalogJob` class file does not exist (deleted). `ProgramHelpers.cs:207` references it (won't compile).
- PostgreSQL migration already adds a `SearchVector` STORED generated tsvector column + GIN index. No rebuild job needed.
- Decision: REMOVE the recurring job registration. PostgreSQL FTS is maintained automatically via the generated column.
- `SearchProductsQueryHandler` references `searchable_vector` column — migration creates `SearchVector` column. Entity has `SearchableVector` property. Need to align: the DB column is `"SearchVector"` (lowercase: `searchvector`). The handler should reference the actual column. Fix handler SQL to use `SearchVector`.
- `GetProductSuggestionsQueryHandler` uses SQL Server `CONTAINSTABLE`/`TOP`. Rewrite to PostgreSQL `to_tsvector`/`LIMIT`.

### Migrations consistency
- Snapshot references `SearchableVector` property NOT configured — EF will map `SearchableVector` property to a column. Migration creates `SearchVector`. Mismatch will cause issues. Need to add `.HasColumnName("SearchVector")` on the property or align names.

### Connection string
- `DesignTimeDbContextFactory` hardcodes `novashop-dev` password. Replace with config/env.
- `appsettings.json` DefaultConnection = localdb. Must change to PostgreSQL.

### Gateway routing
- `appsettings.Development.json` ReverseProxy cluster = `http://localhost:5006` — but API runs on 5003/5000. This is WRONG. Should be `http://localhost:5003` in Development. Production uses compose env override `http://novashop-api:5000` — that is correct via env var.
- Gateway `Program.cs:30`: commented-out `Authority` line. Clean it up.

### JWT
- `appsettings.json`: Key is dev placeholder, read from config. In dev, rely on User Secrets. In prod, env var. Already config-driven. Fix gateway appsettings.Development.json which has no Jwt:Key (relies on user-secrets). Fine. Verify both use same key.

### CORS
- Already config-driven + env var override. Looks correct.

## Execution Order
1. Fix `.csproj` files (remove SQL Server packages, add Hangfire.PostgreSQL).
2. Fix `appsettings.json` / `appsettings.Development.json` connection strings.
3. Fix `docker-compose.yml` / `docker-compose.prod.yml` (PostgreSQL container).
4. Fix `ProgramHelpers.cs` (remove RebuildFtsCatalogJob, fix health-check SQL).
5. Fix `GetProductSuggestionsQueryHandler.cs` (PostgreSQL FTS).
6. Align `SearchProductsQueryHandler.cs` column name + entity mapping.
7. Fix `NovShop.Infrastructure.csproj` (remove SqlServer EF package).
8. Fix test files (Testcontainers.PostgreSql).
9. Fix ETL tool (or leave — it's a dev tool, not part of build target per task).
10. Fix gateway appsettings (ReverseProxy address in dev).
11. Build + test.

## Decision on ETL tool
ETL tool is a maintenance script, not part of the build/runtime. Task says "do not commit/push" and "build NovaShop.Api + NovaShop.ApiGateway". ETL not in those targets. Will leave SQL Server source as-is (it's a one-time migration tool) unless it breaks build. It is in `tools/` — not referenced by main build. Leave it.
