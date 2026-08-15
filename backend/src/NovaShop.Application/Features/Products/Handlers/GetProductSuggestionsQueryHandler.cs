using System.Data;
using System.Text.RegularExpressions;
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

        var ftsQuery = $"\"{Regex.Replace(raw, @"[^\w\s]", "")}*\"";
        var sql = @"
SELECT TOP (@Max) p.Id, p.Name, p.Price, p.ImageUrl
FROM Products p
INNER JOIN CONTAINSTABLE(Products, Name, @ftsQuery, @Max) ft
    ON p.Id = ft.[KEY]
ORDER BY ft.RANK DESC;
";
        try
        {
            var items = await _connection.QueryAsync<ProductSuggestion>(
                sql, new { ftsQuery, Max = request.MaxResults });
            return items.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suggestions query failed: {Query}", ftsQuery);
            return [];
        }
    }
}
