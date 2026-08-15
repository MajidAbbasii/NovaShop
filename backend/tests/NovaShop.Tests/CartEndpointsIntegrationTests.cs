using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Carts.Commands;
using NovaShop.Application.Features.Carts.Dtos;
using NovaShop.Application.Features.Products.Commands;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class CartEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public CartEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("cart-user-1", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/cart?userId={user.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddToCart_WithValidProduct_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("cart-user-2", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Cart Test Product", 29.99m, "https://example.com/cart.jpg", 50, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "For cart testing"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var productId = await createResponse.Content.ReadFromJsonAsync<int>();

        var addToCartRequest = new AddToCartCommand(user.Id, productId, 2);

        var response = await _client.PostAsJsonAsync("/api/cart", addToCartRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
