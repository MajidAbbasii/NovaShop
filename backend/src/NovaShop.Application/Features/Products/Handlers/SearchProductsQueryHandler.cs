using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Dtos;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Common;
using NovaShop.Domain.Common;

namespace NovaShop.Application.Features.Products.Handlers;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedResult<ProductSearchDto>>
{
    private readonly IDbConnection _connection;
    private readonly ICacheService _cache;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    // Stop words stripped from FTS query
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "it", "as", "be", "are", "was",
        "not", "no", "so", "if", "do", "up", "al", "la", "le", "de", "da"
    };

    public SearchProductsQueryHandler(IDbConnection connection, ICacheService cache, ILogger<SearchProductsQueryHandler> logger)
    {
        _connection = connection;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<ProductSearchDto>> Handle(SearchProductsQuery request, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("SearchProducts");

        var raw = request.Query.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return new PagedResult<ProductSearchDto>([], 0, request.PageNumber, request.PageSize, 0);

        var cacheKey = $"search_{raw}_{request.PageNumber}_{request.PageSize}_{request.SortBy}";

        if (await _cache.GetAsync<PagedResult<ProductSearchDto>>(cacheKey) is { } cached)
            return cached;

        var result = await RunSearchQuery(raw, request.PageNumber, request.PageSize, request.SortBy);

        // Apply highlights & snippets in C#
        foreach (var item in result.Items)
        {
            item.Description = TruncateWords(item.Description, 150);
            item.Name = HighlightText(item.Name, raw);
        }

        // Cache 5 minutes
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    private async Task<PagedResult<ProductSearchDto>> RunSearchQuery(string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        try
        {
            return await RunFullTextSearch(ftsQuery, pageNumber, pageSize, sortBy);
        }
        catch (Exception ex)
        {
            // Full-text search unavailable; fall back to a LIKE search so the endpoint still returns results
            _logger.LogWarning(ex, "Full-text search failed; falling back to LIKE search.");
            return await RunLikeSearch(ftsQuery, pageNumber, pageSize, sortBy);
        }
    }

    private async Task<PagedResult<ProductSearchDto>> RunFullTextSearch(string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        var offset = (pageNumber - 1) * pageSize;

        // PostgreSQL full-text search against the stored generated tsvector column.
        // 'simple' config matches the column's to_tsvector('simple', ...) generation.
        // plainto_tsquery safely converts raw user input to a tsquery.
        // All identifiers are quoted to match the PascalCase schema.
        var sql = $@"
WITH Ranked AS (
    SELECT
        p.""Id"", p.""Name"", p.""Description"", p.""Price"", p.""OriginalPrice"",
        p.""ImageUrl"", p.""Rating"", p.""Stock"",
        CASE WHEN p.""Stock"" > 0 THEN 1 ELSE 0 END AS ""IsAvailable"",
        ts_rank_cd(p.""SearchVector"", plainto_tsquery('simple', @ftsQuery)) AS rank
    FROM ""Products"" p
    WHERE p.""SearchVector"" IS NOT NULL
      AND plainto_tsquery('simple', @ftsQuery) @@ p.""SearchVector""
)
SELECT
    ""Id"", ""Name"", ""Description"", ""Price"", ""OriginalPrice"", ""ImageUrl"",
    ""Rating"", ""Stock"", ""IsAvailable"", rank AS ""Rank""
FROM Ranked
{BuildSortClause(sortBy)}
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;

SELECT COUNT(*)
FROM ""Products""
WHERE ""SearchVector"" IS NOT NULL
  AND plainto_tsquery('simple', @ftsQuery) @@ ""SearchVector"";";

        var multi = await _connection.QueryMultipleAsync(sql, new { ftsQuery });
        var items = (await multi.ReadAsync<ProductSearchDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<ProductSearchDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    private async Task<PagedResult<ProductSearchDto>> RunLikeSearch(string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        var offset = (pageNumber - 1) * pageSize;

        var tokens = Regex.Matches(ftsQuery, @"([^\s]+)")
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count == 0) return new PagedResult<ProductSearchDto>([], 0, pageNumber, pageSize, 0);

        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        for (var i = 0; i < tokens.Count; i++)
        {
            var paramName = "@q" + i;
            parameters.Add(paramName, "%" + tokens[i] + "%");
            conditions.Add("(p.\"Name\" ILIKE " + paramName + " OR p.\"Description\" ILIKE " + paramName + ")");
        }
        var where = string.Join(" OR ", conditions);

        var likeSql = @"
WITH Matched AS (
    SELECT
        p.""Id"", p.""Name"", p.""Description"", p.""Price"", p.""OriginalPrice"",
        p.""ImageUrl"", p.""Rating"", p.""Stock"",
        CASE WHEN p.""Stock"" > 0 THEN 1 ELSE 0 END AS ""IsAvailable""
    FROM ""Products"" p
    WHERE " + where + @"
)
SELECT
    ""Id"", ""Name"", ""Description"", ""Price"", ""OriginalPrice"", ""ImageUrl"",
    ""Rating"", ""Stock"", ""IsAvailable"", 1 AS ""Rank""
FROM Matched
" + BuildSortClause(sortBy) + @"
OFFSET " + offset + " ROWS FETCH NEXT " + pageSize + @" ROWS ONLY;

SELECT COUNT(*) FROM ""Products"" WHERE " + where + ";";

        var multi = await _connection.QueryMultipleAsync(likeSql, parameters);
        var items = (await multi.ReadAsync<ProductSearchDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<ProductSearchDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    private static string BuildSortClause(string sortBy)
    {
        return sortBy switch
        {
            "price_asc" => "ORDER BY \"Price\" ASC",
            "price_desc" => "ORDER BY \"Price\" DESC",
            "name" => "ORDER BY \"Name\"",
            _ => "ORDER BY \"Rank\" DESC"
        };
    }

    internal static string BuildFtsQuery(string raw)
    {
        var cleaned = Regex.Replace(raw, @"[^\w\s]", " ");
        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .Select(t => "[" + t + "]")
            .ToList();

        if (tokens.Count == 0)
            return "[" + Regex.Replace(raw, @"[^\w\s]", "").Trim() + "]";

        return string.Join(" OR ", tokens);
    }

    internal static string HighlightText(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return text;

        var pattern = string.Join("|",
            query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));
        if (string.IsNullOrEmpty(pattern)) return text;

        return Regex.Replace(text, "(" + pattern + ")", "<mark>$1</mark>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string TruncateWords(string text, int maxLen)
    {
        if (text.Length <= maxLen) return text;
        var truncated = text[..maxLen];
        var lastSpace = truncated.LastIndexOf(' ');
        return lastSpace > 0 ? truncated[..lastSpace] + "..." : truncated + "...";
    }
}
