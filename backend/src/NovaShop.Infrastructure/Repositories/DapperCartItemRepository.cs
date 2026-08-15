using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperCartItemRepository : ICartItemRepository
{
    private readonly string _connectionString;

    public DapperCartItemRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<CartItem?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM CartItems WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<CartItem>(sql, new { Id = id });
    }

    public async Task<List<CartItem>> GetByCartIdAsync(int cartId)
    {
        const string sql = "SELECT * FROM CartItems WHERE CartId = @CartId";
        using var connection = new SqlConnection(_connectionString);
        var items = await connection.QueryAsync<CartItem>(sql, new { CartId = cartId });
        return items.ToList();
    }

    public async Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId)
    {
        const string sql = "SELECT * FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<CartItem>(sql, new { CartId = cartId, ProductId = productId });
    }

    public async Task<int> AddAsync(CartItem cartItem)
    {
        const string sql = @"
            INSERT INTO CartItems (CartId, ProductId, Quantity, UnitPrice)
            VALUES (@CartId, @ProductId, @Quantity, @UnitPrice);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, cartItem);
        cartItem.Id = id;
        return id;
    }

    public async Task UpdateAsync(CartItem cartItem)
    {
        const string sql = @"
            UPDATE CartItems 
            SET Quantity = @Quantity
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, cartItem);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM CartItems WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM CartItems WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
