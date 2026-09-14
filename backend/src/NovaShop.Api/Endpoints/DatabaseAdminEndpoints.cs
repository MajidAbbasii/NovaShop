using System.Data;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

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
        var admin = app.MapGroup("/api/admin/database").RequireAuthorization("AdminOnly");

        admin.MapPost("/sql", async (
            DatabaseSqlRequest request,
            IConfiguration config,
            IDbConnection db,
            CancellationToken ct) =>
        {
            // --- Access key gate ---
            var configuredKey = config["DatabaseAdmin:AccessKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.AccessKey))
                return Results.Forbid();

            var provided = Encoding.UTF8.GetBytes(request.AccessKey);
            var expected = Encoding.UTF8.GetBytes(configuredKey);
            if (provided.Length != expected.Length ||
                !CryptographicOperations.FixedTimeEquals(provided, expected))
                return Results.Forbid();

            // --- SQL validation ---
            if (string.IsNullOrWhiteSpace(request.Sql))
                return Results.BadRequest(new { error = "SQL is required" });

            var sql = request.Sql.Trim();
            if (sql.EndsWith(';'))
                sql = sql[..^1].Trim();
            if (string.IsNullOrWhiteSpace(sql))
                return Results.BadRequest(new { error = "SQL is empty" });

            // Block comments
            if (sql.Contains("--") || sql.Contains("/*") || sql.Contains("*/"))
                return Results.BadRequest(new { error = "SQL comments are not allowed" });

            // Block multiple statements
            if (sql.Contains(';'))
                return Results.BadRequest(new { error = "Multiple SQL statements are not allowed" });

            // Extract and validate first keyword
            var keyword = FirstKeyword(sql);
            if (BlockedCommands.Contains(keyword))
                return Results.BadRequest(new { error = $"Command '{keyword}' is not allowed" });
            if (!IsAllowed(keyword))
                return Results.BadRequest(new { error = $"Command '{keyword}' is not allowed. Only SELECT, INSERT, UPDATE are permitted." });

            // --- Execute ---
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

        return app;
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
        // Require LIMIT in SELECT
        if (!sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "SELECT requires a LIMIT clause (max 1000)" });

        // Enforce max LIMIT 1000
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

/// <summary>
/// Temporary DTO for database SQL endpoint. REMOVE after migration is complete.
/// </summary>
public record DatabaseSqlRequest
{
    public string? AccessKey { get; init; }
    public string? Sql { get; init; }
}
