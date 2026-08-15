using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;
using NovaShop.Domain.Entities;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace NovaShop.Tests;

public class NovaShopIntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServerContainer;
    private readonly RedisContainer _redisContainer;
    private string _sqlConnectionString = null!;
    private string _redisConnectionString = null!;

    public NovaShopIntegrationTestFixture()
    {
        var sqlBuilder = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithCleanUp(true);

        var redisBuilder = new RedisBuilder()
            .WithImage("redis:latest")
            .WithCleanUp(true);

        _sqlServerContainer = sqlBuilder.Build();
        _redisContainer = redisBuilder.Build();

        Console.WriteLine("Testcontainers configured for SQL Server and Redis");
    }

    public string GetSqlConnectionString()
    {
        if (string.IsNullOrEmpty(_sqlConnectionString))
        {
            _sqlConnectionString = _sqlServerContainer.GetConnectionString();
        }
        return _sqlConnectionString;
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
        optionsBuilder.UseSqlServer(GetSqlConnectionString());
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
        await _sqlServerContainer.StartAsync();
        await _redisContainer.StartAsync();

        Console.WriteLine($"SQL Server container started on port {_sqlServerContainer.GetMappedPublicPort(1433)}");
        Console.WriteLine($"Redis container started on port {_redisContainer.GetMappedPublicPort(6379)}");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping Testcontainers containers...");
        await _sqlServerContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _sqlServerContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
