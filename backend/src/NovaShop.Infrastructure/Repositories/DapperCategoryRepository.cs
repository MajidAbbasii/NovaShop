using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperCategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public DapperCategoryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResult<Category>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT * FROM Categories
            ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        const string countSql = "SELECT COUNT(*) FROM Categories";

        var categories = await connection.QueryAsync<Category>(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Category>(categories.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Categories WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { Id = id });
    }

    public async Task<List<Category>> GetByCategoryNameAsync(string name)
    {
        const string sql = "SELECT * FROM Categories WHERE Name LIKE @Name";
        using var connection = new SqlConnection(_connectionString);
        var categories = await connection.QueryAsync<Category>(sql, new { Name = "%" + name + "%" });
        return categories.ToList();
    }

    public async Task<List<Category>> GetSubCategoriesAsync(int parentCategoryId)
    {
        const string sql = "SELECT * FROM Categories WHERE ParentCategoryId = @ParentCategoryId";
        using var connection = new SqlConnection(_connectionString);
        var categories = await connection.QueryAsync<Category>(sql, new { ParentCategoryId = parentCategoryId });
        return categories.ToList();
    }

    public async Task<int> AddAsync(Category category)
    {
        const string sql = @"
            INSERT INTO Categories (Name, Description, ImageUrl, ParentCategoryId)
            VALUES (@Name, @Description, @ImageUrl, @ParentCategoryId);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, category);
        category.Id = id;
        return id;
    }

    public async Task UpdateAsync(Category category)
    {
        const string sql = @"
            UPDATE Categories 
            SET Name = @Name, Description = @Description, ImageUrl = @ImageUrl, ParentCategoryId = @ParentCategoryId
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, category);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Categories WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Categories WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
