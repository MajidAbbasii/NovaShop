using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Application.Features.Reviews.Commands;
using NovaShop.Application.Features.Reviews.Dtos;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class ReviewEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public ReviewEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReviewsForProduct_Empty_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/products/1/reviews");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.Content.ReadFromJsonAsync<IEnumerable<ReviewDto>>();
        reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateReview_WithValidData_ReturnsCreated()
    {
        var user = await _factory.CreateTestUserAsync("review-user-1", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createProductRequest = new CreateProductCommand("Review Product", 49.99m, "https://example.com/test.jpg", 25, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var createProductResponse = await _client.PostAsJsonAsync("/api/products", createProductRequest);
        var productId = await createProductResponse.Content.ReadFromJsonAsync<int>();

        var createReviewRequest = new CreateReviewCommand
        {
            ProductId = productId,
            UserId = user.Id,
            Rating = 5,
            Comment = "Excellent product!"
        };

        var response = await _client.PostAsJsonAsync("/api/reviews", createReviewRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var reviewId = await response.Content.ReadFromJsonAsync<int>();
        reviewId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteReview_WithValidId_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("review-user-2", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createProductRequest = new CreateProductCommand("Delete Review Product", 39.99m, "https://example.com/test.jpg", 20, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var createProductResponse = await _client.PostAsJsonAsync("/api/products", createProductRequest);
        var productId = await createProductResponse.Content.ReadFromJsonAsync<int>();

        var createReviewRequest = new CreateReviewCommand
        {
            ProductId = productId,
            UserId = user.Id,
            Rating = 4,
            Comment = "Good product"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/reviews", createReviewRequest);
        var reviewId = await createResponse.Content.ReadFromJsonAsync<int>();

        var deleteResponse = await _client.DeleteAsync($"/api/reviews/{reviewId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/products/1/reviews");
        var reviews = await getResponse.Content.ReadFromJsonAsync<IEnumerable<ReviewDto>>();
        reviews.Should().BeEmpty();
    }
}
