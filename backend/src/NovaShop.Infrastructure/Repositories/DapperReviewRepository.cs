using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperReviewRepository : IReviewRepository
{
    private readonly string _connectionString;

    public DapperReviewRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<Review>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT * FROM Reviews
            ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        const string countSql = "SELECT COUNT(*) FROM Reviews";

        var reviews = await connection.QueryAsync<Review>(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Review>(reviews.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Reviews WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Review>(sql, new { Id = id });
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId)
    {
        const string sql = "SELECT * FROM Reviews WHERE ProductId = @ProductId ORDER BY CreatedAt DESC";
        using var connection = new SqlConnection(_connectionString);
        var reviews = await connection.QueryAsync<Review>(sql, new { ProductId = productId });
        return reviews.ToList();
    }

    public async Task<List<Review>> GetByUserIdAsync(int userId)
    {
        const string sql = "SELECT * FROM Reviews WHERE UserId = @UserId ORDER BY CreatedAt DESC";
        using var connection = new SqlConnection(_connectionString);
        var reviews = await connection.QueryAsync<Review>(sql, new { UserId = userId });
        return reviews.ToList();
    }

    public async Task<int> AddAsync(Review review)
    {
        const string sql = @"
            INSERT INTO Reviews (ProductId, UserId, Rating, Comment, CreatedAt)
            VALUES (@ProductId, @UserId, @Rating, @Comment, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, review);
        review.Id = id;
        return id;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Reviews WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
