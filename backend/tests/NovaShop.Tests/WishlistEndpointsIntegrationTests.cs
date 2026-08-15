using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Wishlists.Commands;
using NovaShop.Application.Features.Wishlists.Dtos;
using NovaShop.Application.Features.Products.Commands;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class WishlistEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public WishlistEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<int> CreateTestProductAsync(HttpClient client, string name, decimal price, string description = "Test Description")
    {
        var categoryId = await _factory.GetFirstCategoryIdAsync();
        var request = new CreateProductCommand(name, price, "https://example.com/test.jpg", 10, categoryId)
        {
            Description = description
        };
        var response = await client.PostAsJsonAsync("/api/products", request);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    [Fact]
    public async Task GetWishlist_Empty_ReturnsEmptyList()
    {
        var user = await _factory.CreateTestUserAsync("wishlist-user-1", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/wishlist");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await response.Content.ReadFromJsonAsync<PagedResult<WishlistItemDto>>();
        wishlist!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToWishlist_WithValidData_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("wishlist-user-2", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync(_client, "Wishlist Test Product", 59.99m);

        var addToWishlistRequest = new AddToWishlistCommand
        {
            UserId = user.Id,
            ProductId = productId,
            Note = "Test note"
        };

        var response = await _client.PostAsJsonAsync("/api/wishlist", addToWishlistRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveFromWishlist_WithValidData_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("wishlist-user-3", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync(_client, "Remove Test Product", 39.99m);

        var addToWishlistRequest = new AddToWishlistCommand
        {
            UserId = user.Id,
            ProductId = productId
        };

        await _client.PostAsJsonAsync("/api/wishlist", addToWishlistRequest);

        var removeResponse = await _client.DeleteAsync($"/api/wishlist/{productId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetWishlist_WithItems_ReturnsItems()
    {
        var user = await _factory.CreateTestUserAsync("wishlist-user-4", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync(_client, "Get Wishlist Product", 79.99m);

        var addToWishlistRequest = new AddToWishlistCommand
        {
            UserId = user.Id,
            ProductId = productId
        };

        await _client.PostAsJsonAsync("/api/wishlist", addToWishlistRequest);

        var getResponse = await _client.GetAsync("/api/wishlist");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await getResponse.Content.ReadFromJsonAsync<PagedResult<WishlistItemDto>>();
        wishlist!.Items.Should().HaveCount(1);
        wishlist.Items[0].ProductName.Should().Be("Get Wishlist Product");
    }

    [Fact]
    public async Task CheckWishlistItem_ProductInWishlist_ReturnsTrue()
    {
        var user = await _factory.CreateTestUserAsync("wishlist-user-5", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateTestProductAsync(_client, "Check Wishlist Product", 69.99m);

        var addToWishlistRequest = new AddToWishlistCommand
        {
            UserId = user.Id,
            ProductId = productId
        };

        await _client.PostAsJsonAsync("/api/wishlist", addToWishlistRequest);

        var checkResponse = await _client.GetAsync($"/api/wishlist/check/{productId}");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var isInWishlist = await checkResponse.Content.ReadFromJsonAsync<bool>();
        isInWishlist.Should().BeTrue();
    }
}
