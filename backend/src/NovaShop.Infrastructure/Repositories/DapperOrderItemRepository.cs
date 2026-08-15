using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperOrderItemRepository : IOrderItemRepository
{
    private readonly string _connectionString;

    public DapperOrderItemRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<OrderItem>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT * FROM OrderItems
            ORDER BY Id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        const string countSql = "SELECT COUNT(*) FROM OrderItems";

        var items = await connection.QueryAsync<OrderItem>(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<OrderItem>(items.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<OrderItem?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM OrderItems WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<OrderItem>(sql, new { Id = id });
    }

    public async Task<List<OrderItem>> GetByOrderIdAsync(int orderId)
    {
        const string sql = "SELECT * FROM OrderItems WHERE OrderId = @OrderId";
        using var connection = new SqlConnection(_connectionString);
        var items = await connection.QueryAsync<OrderItem>(sql, new { OrderId = orderId });
        return items.ToList();
    }

    public async Task<int> AddAsync(OrderItem orderItem)
    {
        const string sql = @"
            INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, orderItem);
        orderItem.Id = id;
        return id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM OrderItems WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
