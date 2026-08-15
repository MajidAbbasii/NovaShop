using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovaShop.Api;
using NovaShop.Api.Extensions;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;
using NovaShop.Infrastructure.Services;
using System.Net.Http.Headers;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace NovaShop.Tests;

public class IntegrationWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    private RedisContainer? _redisContainer;

    public async Task InitializeAsync()
    {
        var sqlBuilder = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithCleanUp(true);

        var redisBuilder = new RedisBuilder()
            .WithImage("redis:latest")
            .WithCleanUp(true);

        _sqlContainer = sqlBuilder.Build();
        _redisContainer = redisBuilder.Build();

        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        var sqlPort = _sqlContainer.GetMappedPublicPort(1433);
        var redisPort = _redisContainer.GetMappedPublicPort(6379);

        Environment.SetEnvironmentVariable("SQL_CONNECTION_STRING", $"Server=localhost,{sqlPort};Database=NovaShopTest;User Id=sa;Password={MsSqlBuilder.DefaultPassword};TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", $"localhost:{redisPort}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddEnvironmentVariables();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SQL_CONNECTION_STRING"] = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING") ?? "",
                ["REDIS_CONNECTION_STRING"] = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? ""
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            var sqlConnectionString = context.Configuration.GetConnectionString("DefaultConnection")
                                      ?? Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");

            services.AddDbContext<NovaShopDbContext>(options =>
                options.UseSqlServer(sqlConnectionString));

            // Inject mock payment gateway; AddNovaShopServices already registers the default
            // but we override for test control
            services.AddSingleton<IPaymentGateway, MockPaymentGateway>();

            services.AddNovaShopServices(context.Configuration);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }

    public HttpClient CreateAuthenticatedClient(string username, string role)
    {
        var client = CreateClient();
        var token = GenerateTestToken(username, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    internal async Task<User> CreateTestUserAsync(string username, string role)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovaShopDbContext>();

        var user = new User
        {
            Id = string.IsNullOrEmpty(username) ? 0 : username.Length > 0 ? username.Length.GetHashCode() : 0,
            Username = username,
            Email = string.IsNullOrEmpty(username) ? string.Empty : username,
            Role = role,
            IsActive = true
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
    internal async Task<string> GetAuthTokenAsync(User user)
    {
        var token = GenerateTestToken(user.Username, user.Role);
        return token;
    }

    internal async Task<int> GetFirstCategoryIdAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovaShopDbContext>();
        var category = await dbContext.Categories.FirstOrDefaultAsync();
        return category?.Id ?? 1;
    }

    private static string GenerateTestToken(string username, string role)
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("TestSuperSecretKeyThatIsLongEnoughForHmacSha25612345!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "NovaShop",
            audience: "NovaShop",
            claims: new[] { new System.Security.Claims.Claim("sub", username), new System.Security.Claims.Claim("role", role) },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeContainersAsync();
    }

    private async Task DisposeContainersAsync()
    {
        if (_sqlContainer != null)
        {
            await _sqlContainer.StopAsync();
            await _sqlContainer.DisposeAsync();
        }
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }
}
