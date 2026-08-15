using MediatR;
using NovaShop.Application.Features.Products.Dtos;

namespace NovaShop.Application.Features.Products.Queries;

public record GetProductSuggestionsQuery(string Query, int MaxResults = 8)
    : IRequest<List<ProductSuggestion>>;
