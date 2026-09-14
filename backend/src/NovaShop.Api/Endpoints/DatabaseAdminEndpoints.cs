using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Api.Endpoints;

// ---- Temporary database admin endpoint — REMOVE after migration is complete ----
// Allows controlled SELECT/INSERT/UPDATE against the PostgreSQL database via the API.
// Protected by AdminOnly policy + DatabaseAdmin:AccessKey configuration secret.
public static class DatabaseAdminEndpoints
{
    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "GRANT", "REVOKE",
        "COMMENT", "COPY", "VACUUM", "ANALYZE", "DO", "EXECUTE", "SET",
        "RESET", "SHOW", "LISTEN", "NOTIFY", "BEGIN", "COMMIT", "ROLLBACK",
        "SAVEPOINT", "RELEASE", "LOCK", "UNLOCK", "EXPLAIN", "PREPARE",
        "DEALLOCATE", "DECLARE", "FETCH", "MOVE", "CLOSE", "IMPORT", "EXPORT",
    };

    public static IEndpointRouteBuilder MapDatabaseAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // ---- SQL endpoint (Admin JWT + access key) ----
        var admin = app.MapGroup("/api/admin/database").RequireAuthorization("AdminOnly");

        admin.MapPost("/sql", async (
            DatabaseSqlRequest request,
            IConfiguration config,
            IDbConnection db,
            CancellationToken ct) =>
        {
            var accessKeyError = ValidateAccessKey(config, request.AccessKey);
            if (accessKeyError is not null) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.Sql))
                return Results.BadRequest(new { error = "SQL is required" });

            var sql = request.Sql.Trim();
            if (sql.EndsWith(';'))
                sql = sql[..^1].Trim();
            if (string.IsNullOrWhiteSpace(sql))
                return Results.BadRequest(new { error = "SQL is empty" });

            if (sql.Contains("--") || sql.Contains("/*") || sql.Contains("*/"))
                return Results.BadRequest(new { error = "SQL comments are not allowed" });

            if (sql.Contains(';'))
                return Results.BadRequest(new { error = "Multiple SQL statements are not allowed" });

            var keyword = FirstKeyword(sql);
            if (BlockedCommands.Contains(keyword))
                return Results.BadRequest(new { error = $"Command '{keyword}' is not allowed" });
            if (!IsAllowed(keyword))
                return Results.BadRequest(new { error = $"Command '{keyword}' is not allowed. Only SELECT, INSERT, UPDATE are permitted." });

            try
            {
                if (db.State != ConnectionState.Open)
                    await ((NpgsqlConnection)db).OpenAsync(ct);

                return keyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase)
                    ? await RunSelectAsync((NpgsqlConnection)db, sql, ct)
                    : await RunMutateAsync((NpgsqlConnection)db, sql, ct);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("AdminDatabaseSql");

        // ---- Bootstrap endpoint (access key only, no JWT) ----
        app.MapPost("/api/admin/database/bootstrap", async (
            BootstrapRequest request,
            IConfiguration config,
            NovaShopDbContext db,
            CancellationToken ct) =>
        {
            var accessKeyError = ValidateAccessKey(config, request.AccessKey);
            if (accessKeyError is not null) return Results.Forbid();

            if (request.AdminUser is null && (request.Translations is null || request.Translations.Count == 0))
                return Results.BadRequest(new { error = "At least one of adminUser or translations must be provided" });

            // Validate admin request if present
            if (request.AdminUser is not null)
            {
                if (string.IsNullOrWhiteSpace(request.AdminUser.Username) ||
                    string.IsNullOrWhiteSpace(request.AdminUser.Email) ||
                    string.IsNullOrWhiteSpace(request.AdminUser.PasswordHash))
                {
                    return Results.BadRequest(new { error = "adminUser requires username, email, and passwordHash" });
                }
            }

            // Validate translations if present
            if (request.Translations is not null)
            {
                for (var i = 0; i < request.Translations.Count; i++)
                {
                    var t = request.Translations[i];
                    if (string.IsNullOrWhiteSpace(t.Key) || string.IsNullOrWhiteSpace(t.Locale) || string.IsNullOrWhiteSpace(t.Value))
                        return Results.BadRequest(new { error = $"Translation record {i} is missing required fields (key, locale, value)" });
                }
            }

            // Idempotent: check current state
            var usersExist = await db.Users.AnyAsync(ct);
            var translationsExist = await db.Translations.AnyAsync(ct);

            var adminInserted = false;
            var translationsInserted = 0;

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // Insert admin only if Users table is empty
                if (request.AdminUser is not null && !usersExist)
                {
                    var user = new User
                    {
                        Username = request.AdminUser.Username,
                        Email = request.AdminUser.Email,
                        PasswordHash = request.AdminUser.PasswordHash,
                        Role = User.RoleAdmin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };
                    db.Users.Add(user);
                    adminInserted = true;
                }

                // Import translations only if Translations table is empty
                if (request.Translations is not null && request.Translations.Count > 0 && !translationsExist)
                {
                    var entities = request.Translations.Select(t => new Translation
                    {
                        Key = t.Key!,
                        Locale = t.Locale!,
                        Value = t.Value!,
                        Namespace = t.Namespace,
                        Description = t.Description,
                        IsActive = t.IsActive ?? true,
                        CreatedAt = t.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = t.UpdatedAt ?? DateTime.UtcNow,
                        CreatedBy = t.CreatedBy,
                        UpdatedBy = t.UpdatedBy,
                    }).ToList();

                    db.Translations.AddRange(entities);
                    translationsInserted = entities.Count;
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return Results.Ok(new
                {
                    adminInserted,
                    adminSkipped = request.AdminUser is not null && !adminInserted && usersExist,
                    translationsInserted,
                    translationsSkipped = request.Translations is not null &&
                        request.Translations.Count > 0 &&
                        translationsInserted == 0 &&
                        translationsExist,
                });
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        })
        .WithName("AdminDatabaseBootstrap");

        return app;
    }

    // ---- Helpers ----

    /// <summary>Constant-time access key comparison. Returns null on success, "forbidden" on failure.</summary>
    private static string? ValidateAccessKey(IConfiguration config, string? providedKey)
    {
        var configuredKey = config["DatabaseAdmin:AccessKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return "forbidden";

        if (string.IsNullOrWhiteSpace(providedKey))
            return "forbidden";

        var provided = Encoding.UTF8.GetBytes(providedKey);
        var expected = Encoding.UTF8.GetBytes(configuredKey);
        if (provided.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(provided, expected))
            return "forbidden";

        return null;
    }

    private static bool IsAllowed(string keyword) =>
        keyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase) ||
        keyword.Equals("INSERT", StringComparison.OrdinalIgnoreCase) ||
        keyword.Equals("UPDATE", StringComparison.OrdinalIgnoreCase);

    private static string FirstKeyword(string sql)
    {
        var i = 0;
        while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
        var start = i;
        while (i < sql.Length && !char.IsWhiteSpace(sql[i]) && sql[i] != '(' && sql[i] != ';') i++;
        return sql[start..i];
    }

    private static async Task<IResult> RunSelectAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        if (!sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "SELECT requires a LIMIT clause (max 1000)" });

        var upper = sql.ToUpperInvariant();
        var limitIdx = upper.LastIndexOf("LIMIT");
        var afterLimit = sql[(limitIdx + 5)..].TrimStart('(', ' ');
        var limitStr = new string(afterLimit.TakeWhile(c => char.IsDigit(c)).ToArray());
        if (int.TryParse(limitStr, out var limit) && limit > 1000)
            return Results.BadRequest(new { error = "LIMIT cannot exceed 1000" });

        await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 30 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var columns = new List<string>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct) && rows.Count < 1000)
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return Results.Ok(new { type = "select", columns, rows, rowCount = rows.Count });
    }

    private static async Task<IResult> RunMutateAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var txn = await conn.BeginTransactionAsync(ct);
        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn, txn) { CommandTimeout = 30 };
            var affected = await cmd.ExecuteNonQueryAsync(ct);
            await txn.CommitAsync(ct);
            return Results.Ok(new { type = "mutation", affectedRows = affected });
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }
}

// ---- Temporary DTOs — REMOVE after migration is complete ----

public record DatabaseSqlRequest
{
    public string? AccessKey { get; init; }
    public string? Sql { get; init; }
}

public record BootstrapRequest
{
    public string? AccessKey { get; init; }
    public BootstrapAdminUser? AdminUser { get; init; }
    public List<BootstrapTranslation>? Translations { get; init; }
}

public record BootstrapAdminUser
{
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? PasswordHash { get; init; }
}

public record BootstrapTranslation
{
    public string? Key { get; init; }
    public string? Locale { get; init; }
    public string? Value { get; init; }
    public string? Namespace { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
