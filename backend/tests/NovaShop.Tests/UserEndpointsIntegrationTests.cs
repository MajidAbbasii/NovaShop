using System.Net;
using System.Net.Http.Json;
using NovaShop.Domain.Common;
using NovaShop.Application.Features.Users.Commands;
using NovaShop.Application.Features.Users.Dtos;
using Xunit;
using FluentAssertions;

namespace NovaShop.Tests;

[Collection("Integration Tests")]
public class UserEndpointsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationWebApplicationFactory _factory;

    public UserEndpointsIntegrationTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var user = await _factory.CreateTestUserAsync("user-list-1", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        users.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        var user = await _factory.CreateTestUserAsync("user-create-admin", "Admin");
        var token = await _factory.GetAuthTokenAsync(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateUserCommand("newuser", "newuser@test.com")
        {
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "+1234567890"
        };

        var response = await _client.PostAsJsonAsync("/api/users", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = await response.Content.ReadFromJsonAsync<int>();
        userId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        // Create a user via the test factory first, then update via API
        var existingUser = await _factory.CreateTestUserAsync("user-to-update", "User");
        var adminUser = await _factory.CreateTestUserAsync("user-update-admin", "Admin");
        var token = await _factory.GetAuthTokenAsync(adminUser);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateUserCommand
        {
            Id = existingUser.Id,
            Username = "updated-user",
            Email = "updated@test.com"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/users/{existingUser.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsNoContent()
    {
        var userToDelete = await _factory.CreateTestUserAsync("user-to-delete", "User");
        var adminUser = await _factory.CreateTestUserAsync("user-delete-admin", "Admin");
        var token = await _factory.GetAuthTokenAsync(adminUser);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{userToDelete.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
