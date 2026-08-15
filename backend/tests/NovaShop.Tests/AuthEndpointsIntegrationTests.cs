using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Entities;
using NovaShop.Application.Features.Auth.Commands;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class AuthEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public AuthEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var loginCommand = new LoginCommand("admin", "123456");

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        var loginCommand = new LoginCommand("wronguser", "wrongpass");

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}