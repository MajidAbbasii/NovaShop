using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Application.Jobs;

/// <summary>Hangfire job to rebuild the full-text search catalog periodically.</summary>
public class RebuildFtsCatalogJob
{
    private readonly NovaShopDbContext _context;
    private readonly ILogger<RebuildFtsCatalogJob> _logger;

    public RebuildFtsCatalogJob(NovaShopDbContext context, ILogger<RebuildFtsCatalogJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RebuildAsync(CancellationToken ct)
    {
        if (!await IsFullTextSupportedAsync(ct))
        {
            _logger.LogWarning("Full-text search is not available on this SQL Server instance (e.g. LocalDB user instance). Skipping catalog rebuild.");
            return;
        }

        _logger.LogInformation("Starting full-text catalog rebuild...");

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "ALTER FULLTEXT CATALOG NovaShopCatalog REBUILD;", ct);
            _logger.LogInformation("Full-text catalog rebuilt successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild full-text catalog.");
        }
    }

    public async Task PopulateAsync(CancellationToken ct)
    {
        if (!await IsFullTextSupportedAsync(ct))
        {
            _logger.LogWarning("Full-text search is not available on this SQL Server instance. Skipping index population.");
            return;
        }

        _logger.LogInformation("Starting full-text index population...");

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "ALTER FULLTEXT INDEX ON Products START FULL POPULATION;", ct);
            _logger.LogInformation("Full-text index population started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start full-text index population.");
        }
    }

    private async Task<bool> IsFullTextSupportedAsync(CancellationToken ct)
    {
        try
        {
            var result = await _context.Database
                .SqlQueryRaw<bool>(
                    "SELECT CASE WHEN FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [Value]")
                .SingleOrDefaultAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine full-text support; assuming unavailable.");
            return false;
        }
    }
}