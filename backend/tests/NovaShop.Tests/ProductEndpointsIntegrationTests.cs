using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Products.Commands;
using NovaShop.Application.Features.Products.Dtos;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class ProductEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public ProductEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_Empty_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        products!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        var user = await _factory.CreateTestUserAsync("product-create-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Test Product", 99.99m, "https://example.com/test.jpg", 100, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var response = await _client.PostAsJsonAsync("/api/products", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var productId = await response.Content.ReadFromJsonAsync<int>();
        productId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        var user = await _factory.CreateTestUserAsync("product-view-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Test Product View", 49.99m, "https://example.com/view.jpg", 50, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var productId = await createResponse.Content.ReadFromJsonAsync<int>();

        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        product!.Id.Should().Be(productId);
        product.Name.Should().Be("Test Product View");
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("product-update-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Test Product Update", 39.99m, "https://example.com/update.jpg", 25, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var productId = await createResponse.Content.ReadFromJsonAsync<int>();

        var updateRequest = new UpdateProductCommand
        {
            Id = productId,
            Name = "Updated Product Name",
            Description = "Updated Description",
            Price = 59.99m,
            Stock = 30,
            ImageUrl = "https://example.com/updated.jpg"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{productId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        product!.Name.Should().Be("Updated Product Name");
        product.Price.Should().Be(59.99m);
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContent()
    {
        var user = await _factory.CreateTestUserAsync("product-delete-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Test Product Delete", 29.99m, "https://example.com/delete.jpg", 15, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Test Description"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var productId = await createResponse.Content.ReadFromJsonAsync<int>();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_ReturnsFilteredResults()
    {
        var user = await _factory.CreateTestUserAsync("product-search-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductCommand("Laptop XYZ", 999.99m, "https://example.com/laptop.jpg", 10, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Powerful laptop"
        };
        await _client.PostAsJsonAsync("/api/products", createRequest);

        var createRequest2 = new CreateProductCommand("Phone ABC", 499.99m, "https://example.com/phone.jpg", 20, await _factory.GetFirstCategoryIdAsync())
        {
            Description = "Smart phone"
        };
        await _client.PostAsJsonAsync("/api/products", createRequest2);

        var getResponse = await _client.GetAsync("/api/products?searchTerm=Laptop");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await getResponse.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        products!.Items.Should().HaveCount(1);
        products.Items[0].Name.Should().Contain("Laptop");
    }
}
