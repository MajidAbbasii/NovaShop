using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Features.Products.Dtos;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Common;

namespace NovaShop.Application.Features.Products.Handlers;

public class GetProductSuggestionsQueryHandler
    : IRequestHandler<GetProductSuggestionsQuery, List<ProductSuggestion>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<GetProductSuggestionsQueryHandler> _logger;

    public GetProductSuggestionsQueryHandler(
        IDbConnection connection,
        ILogger<GetProductSuggestionsQueryHandler> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<List<ProductSuggestion>> Handle(
        GetProductSuggestionsQuery request,
        CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("GetProductSuggestions");

        var raw = request.Query.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 2)
            return [];

        // PostgreSQL full-text search: use the generated SearchVector column
        // with a GIN index. plainto_tsquery safely converts raw input to a tsquery.
        // 'simple' config matches the column's to_tsvector('simple', ...) generation.
        var sql = @"
SELECT p.""Id"", p.""Name"", p.""Price"", p.""ImageUrl""
FROM ""Products"" p
WHERE p.""SearchVector"" IS NOT NULL
  AND plainto_tsquery('simple', @ftsQuery) @@ p.""SearchVector""
ORDER BY ts_rank_cd(p.""SearchVector"", plainto_tsquery('simple', @ftsQuery)) DESC
LIMIT @Max;
";

        try
        {
            var items = await _connection.QueryAsync<ProductSuggestion>(
                sql, new { ftsQuery = raw, Max = request.MaxResults });
            return items.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suggestions query failed: {Query}", raw);
            return [];
        }
    }
}
