using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace NovaShop.Tests;

public class NovaShopIntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer;
    private readonly RedisContainer _redisContainer;
    private string _pgConnectionString = null!;
    private string _redisConnectionString = null!;

    public NovaShopIntegrationTestFixture()
    {
        var pgBuilder = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("NovaShopTest")
            .WithUsername("novashop")
            .WithPassword("novashop-test")
            .WithCleanUp(true);

        var redisBuilder = new RedisBuilder()
            .WithImage("redis:latest")
            .WithCleanUp(true);

        _pgContainer = pgBuilder.Build();
        _redisContainer = redisBuilder.Build();

        Console.WriteLine("Testcontainers configured for PostgreSQL and Redis");
    }

    public string GetPgConnectionString()
    {
        if (string.IsNullOrEmpty(_pgConnectionString))
        {
            _pgConnectionString = _pgContainer.GetConnectionString();
        }
        return _pgConnectionString;
    }

    public string GetRedisConnectionString()
    {
        if (string.IsNullOrEmpty(_redisConnectionString))
        {
            _redisConnectionString = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
        }
        return _redisConnectionString;
    }

    public DbContextOptions<NovaShopDbContext> GetDbContextOptions()
    {
        var optionsBuilder = new DbContextOptionsBuilder<NovaShopDbContext>();
        optionsBuilder.UseNpgsql(GetPgConnectionString());
        return optionsBuilder.Options;
    }

    public Mock<IPaymentGateway> GetMockedPaymentGateway()
    {
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.InitiatePaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "TXN-TEST", Authority = "AUTH-TEST", RedirectUrl = "http://gw/test" });
        return mockGateway;
    }

    public Mock<IPublishEndpoint> GetMockedPublishEndpoint()
    {
        return new Mock<IPublishEndpoint>();
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting Testcontainers containers...");
        await _pgContainer.StartAsync();
        await _redisContainer.StartAsync();

        Console.WriteLine($"PostgreSQL container started on port {_pgContainer.GetMappedPublicPort(5432)}");
        Console.WriteLine($"Redis container started on port {_redisContainer.GetMappedPublicPort(6379)}");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping Testcontainers containers...");
        await _pgContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _pgContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
