using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperOrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public DapperOrderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<Order>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT * FROM Orders
            ORDER BY Id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        const string countSql = "SELECT COUNT(*) FROM Orders";

        var orders = await connection.QueryAsync<Order>(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Order>(orders.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Orders WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Order>(sql, new { Id = id });
    }

    public async Task<List<Order>> GetByUserIdAsync(int userId)
    {
        const string sql = "SELECT * FROM Orders WHERE UserId = @UserId ORDER BY CreatedAt DESC";
        using var connection = new SqlConnection(_connectionString);
        var orders = await connection.QueryAsync<Order>(sql, new { UserId = userId });
        return orders.ToList();
    }

    public async Task<List<Order>> GetByStatusAsync(string status)
    {
        const string sql = "SELECT * FROM Orders WHERE Status = @Status ORDER BY CreatedAt DESC";
        using var connection = new SqlConnection(_connectionString);
        var orders = await connection.QueryAsync<Order>(sql, new { Status = status });
        return orders.ToList();
    }

    public async Task<int> AddAsync(Order order)
    {
        const string sql = @"
            INSERT INTO Orders (UserId, TotalAmount, Status, CreatedAt)
            VALUES (@UserId, @TotalAmount, @Status, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, order);
        order.Id = id;
        return id;
    }

    public async Task UpdateAsync(Order order)
    {
        const string sql = @"
            UPDATE Orders 
            SET TotalAmount = @TotalAmount, Status = @Status
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, order);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Orders WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Orders WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
