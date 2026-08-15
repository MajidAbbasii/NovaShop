using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Orders.Dtos;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class OrderEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public OrderEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("order-user-1", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createOrderRequest = new
        {
            UserId = user.Id
        };

        var response = await _client.PostAsJsonAsync("/api/orders", createOrderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("order-user-2", "User");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        orders.Should().NotBeNull();
    }
}
