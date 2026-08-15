using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Categories.Commands;
using NovaShop.Application.Features.Categories.Dtos;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class CategoryEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public CategoryEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<PagedResult<CategoryDto>>();
        categories.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        var user = await _factory.CreateTestUserAsync("cat-create-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateCategoryCommand("Test Category")
        {
            Description = "Test Description",
            ImageUrl = "https://example.com/cat.jpg"
        };

        var response = await _client.PostAsJsonAsync("/api/categories", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoryId = await response.Content.ReadFromJsonAsync<int>();
        categoryId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("cat-update-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateCategoryCommand("Category To Update")
        {
            ImageUrl = "https://example.com/cat.jpg"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/categories", createRequest);
        var categoryId = await createResponse.Content.ReadFromJsonAsync<int>();

        var updateRequest = new UpdateCategoryCommand
        {
            Id = categoryId,
            Name = "Updated Category",
            Description = "Updated Description"
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/categories/{categoryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("cat-delete-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateCategoryCommand("Category To Delete")
        {
            ImageUrl = "https://example.com/cat.jpg"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/categories", createRequest);
        var categoryId = await createResponse.Content.ReadFromJsonAsync<int>();

        var deleteResponse = await _client.DeleteAsync($"/api/categories/{categoryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
