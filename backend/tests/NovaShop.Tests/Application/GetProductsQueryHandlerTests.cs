using Moq;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Handlers;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Application.Mappers;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using Xunit;

namespace NovaShop.Tests.Application;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly ProductMapper _mapper;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _mapper = new ProductMapper();
        _cacheMock = new Mock<ICacheService>();

        _handler = new GetProductsQueryHandler(
            _repositoryMock.Object,
            _mapper,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedProducts()
    {
        // Arrange
        var query = new GetProductsQuery { PageNumber = 1, PageSize = 10 };
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Test", Price = 10m, CategoryId = 1 }
        };
        var pagedResult = new PagedResult<Product>(products, 1, 1, 10, 1);

        _repositoryMock.Setup(r => r.GetAllAsync(null, null, null, null, 1, 10))
            .ReturnsAsync(pagedResult);        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}
