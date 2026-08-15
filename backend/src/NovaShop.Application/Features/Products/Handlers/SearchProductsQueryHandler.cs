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

    public SearchProductsQueryHandler(
        IDbConnection connection,
        ICacheService cache,
        ILogger<SearchProductsQueryHandler> logger)
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

        var ftsQuery = BuildFtsQuery(raw);
        var result = await RunSearchQuery(ftsQuery, request.PageNumber, request.PageSize, request.SortBy);

        // Apply highlights & snippets in C#
        foreach (var item in result.Items)
        {
            item.Description = BuildSnippet(item.Description, raw, 150);
            item.Name = HighlightText(item.Name, raw);
        }

        // Cache 5 minutes
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    private async Task<PagedResult<ProductSearchDto>> RunSearchQuery(
        string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        try
        {
            return await RunFullTextSearch(ftsQuery, pageNumber, pageSize, sortBy);
        }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException)
        {
            // Full-text search is unavailable on this server (e.g. LocalDB user instance).
            // Fall back to a plain LIKE search so the endpoint still returns results.
            _logger.LogWarning(ex, "Full-text search unavailable; falling back to LIKE search.");
            return await RunLikeSearch(ftsQuery, pageNumber, pageSize, sortBy);
        }
    }

    private async Task<PagedResult<ProductSearchDto>> RunFullTextSearch(
        string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        var offset = (pageNumber - 1) * pageSize;
        var sql = $@"
;WITH Ranked AS (
    SELECT
        p.Id, p.Name, p.Description, p.Price, p.OriginalPrice,
        p.ImageUrl, p.Rating, p.Stock,
        CASE WHEN p.Stock > 0 THEN 1 ELSE 0 END AS IsAvailable, ft.RANK
    FROM Products p
    INNER JOIN CONTAINSTABLE(Products, (Name, Description),
        @ftsQuery, 1000) ft ON p.Id = ft.[KEY]
)
SELECT
    Id, Name, Description, Price, OriginalPrice, ImageUrl,
    Rating, Stock, IsAvailable, RANK AS [Rank]
FROM Ranked
{BuildSortClause(sortBy, "Rank")}
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;

SELECT COUNT(*) FROM Products p
INNER JOIN CONTAINSTABLE(Products, (Name, Description),
    @ftsQuery, 1000) ft ON p.Id = ft.[KEY];
";
        var multi = await _connection.QueryMultipleAsync(sql, new { ftsQuery });
        var items = (await multi.ReadAsync<ProductSearchDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<ProductSearchDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    private async Task<PagedResult<ProductSearchDto>> RunLikeSearch(
        string ftsQuery, int pageNumber, int pageSize, string sortBy)
    {
        var offset = (pageNumber - 1) * pageSize;

        // Extract plain terms from the FTS ISABOUT query for LIKE matching.
        var tokens = Regex.Matches(ftsQuery, "\"([^\"]+)\"")
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
            var p = $"@q{i}";
            parameters.Add(p, $"%{tokens[i]}%");
            conditions.Add($"(p.Name LIKE {p} OR p.Description LIKE {p})");
        }
        var where = string.Join(" OR ", conditions);

        var sql = $@"

;WITH Matched AS (
    SELECT
        p.Id, p.Name, p.Description, p.Price, p.OriginalPrice,
        p.ImageUrl, p.Rating, p.Stock,
        CASE WHEN p.Stock > 0 THEN 1 ELSE 0 END AS IsAvailable
    FROM Products p
    WHERE {where}
)
SELECT
    Id, Name, Description, Price, OriginalPrice, ImageUrl,
    Rating, Stock, IsAvailable, 1 AS [Rank]
FROM Matched
{BuildSortClause(sortBy)}
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;

SELECT COUNT(*) FROM Products p
WHERE {where};
";
        var multi = await _connection.QueryMultipleAsync(sql, parameters);
        var items = (await multi.ReadAsync<ProductSearchDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<ProductSearchDto>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    private static string BuildSortClause(string sortBy, string rankColumn = "Rank")
    {
        return sortBy switch
        {
            "price_asc" => "ORDER BY Price ASC",
            "price_desc" => "ORDER BY Price DESC",
            "name" => "ORDER BY Name",
            _ => $"ORDER BY {rankColumn} DESC"
        };
    }

    /// <summary>Build ISABOUT weighted FTS query for CONTAINSTABLE.</summary>
    internal static string BuildFtsQuery(string raw)
    {
        var cleaned = Regex.Replace(raw, @"[^\w\s]", " ");
        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .Select(t => $"FORMSOF(THESAURUS, {t}) OR \"{t}\"")
            .ToList();

        if (tokens.Count == 0)
            return $"\"{Regex.Replace(raw, @"[^\w\s]", "").Trim()}\"";

        // ISABOUT with column weighting: Name 0.8, Description 0.2
        var nameTerms = string.Join(" WEIGHT(0.8), ", tokens.Select(t => $"Name:{t}"));
        var descTerms = string.Join(" WEIGHT(0.2), ", tokens.Select(t => $"Description:{t}"));
        return $"ISABOUT({nameTerms} WEIGHT(0.8), {descTerms} WEIGHT(0.2))";
    }

    /// <summary>Extract a snippet around first match.</summary>
    internal static string? BuildSnippet(string? text, string query, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            var first = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? query;
            idx = text.IndexOf(first, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return TruncateWords(text, maxLen);
        }

        var start = Math.Max(0, idx - maxLen / 2);
        var end = Math.Min(text.Length, idx + query.Length + maxLen / 2);
        var snippet = text[start..end];

        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return HighlightText(snippet, query);
    }

    /// <summary>Wrap matching terms in &lt;mark&gt; tags.</summary>
    internal static string HighlightText(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return text;

        var pattern = string.Join("|",
            query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));
        if (string.IsNullOrEmpty(pattern)) return text;

        return Regex.Replace(text, $"({pattern})", "<mark>$1</mark>",
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