using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public DapperUserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<User>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT * FROM Users
            ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        const string countSql = "SELECT COUNT(*) FROM Users";

        var users = await connection.QueryAsync<User>(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<User>(users.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<int> AddAsync(User user)
    {
        const string sql = @"
            INSERT INTO Users (Username, Email, FirstName, LastName, PhoneNumber, CreatedAt)
            VALUES (@Username, @Email, @FirstName, @LastName, @PhoneNumber, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, user);
        user.Id = id;
        return id;
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE Users 
            SET Username = @Username, Email = @Email, FirstName = @FirstName, 
                LastName = @LastName, PhoneNumber = @PhoneNumber
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Users WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Users WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Username = username });
        return count > 0;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        const string sql = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
