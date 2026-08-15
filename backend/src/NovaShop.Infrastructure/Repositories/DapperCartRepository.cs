using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperCartRepository : ICartRepository
{
    private readonly string _connectionString;

    public DapperCartRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<Cart?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Carts WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Cart>(sql, new { Id = id });
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        const string sql = "SELECT * FROM Carts WHERE UserId = @UserId";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Cart>(sql, new { UserId = userId });
    }

    public async Task<int> AddAsync(Cart cart)
    {
        const string sql = @"
            INSERT INTO Carts (UserId)
            VALUES (@UserId);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, cart);
        cart.Id = id;
        return id;
    }

    public async Task UpdateAsync(Cart cart)
    {
        const string sql = "UPDATE Carts SET UserId = @UserId WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, cart);
    }

    public async Task ClearAsync(int userId)
    {
        const string sql = "DELETE FROM Carts WHERE UserId = @UserId";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Carts WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Carts WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
