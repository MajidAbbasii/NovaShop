using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public DapperProductRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<int> AddAsync(Product product)
    {
        const string sql = @"
            INSERT INTO Products (Name, Price, OriginalPrice, Description, ImageUrl, Stock, Rating, CreatedAt)
            VALUES (@Name, @Price, @OriginalPrice, @Description, @ImageUrl, @Stock, @Rating, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, product);
        product.Id = id;
        return id;
    }

    public async Task<PagedResult<Product>> GetAllAsync(
        string? searchTerm = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? onlyAvailable = null,
        int pageNumber = 1,
        int pageSize = 12,
        int? categoryId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"
            SELECT * FROM Products 
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(searchTerm))
            sql += " AND Name LIKE @SearchTerm";

        if (minPrice.HasValue)
            sql += " AND Price >= @MinPrice";

        if (maxPrice.HasValue)
            sql += " AND Price <= @MaxPrice";

        if (onlyAvailable == true)
            sql += " AND Stock > 0";

        if (categoryId.HasValue)
            sql += " AND CategoryId = @CategoryId";

        sql += " ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var parameters = new
        {
            SearchTerm = "%" + searchTerm + "%",
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Offset = (pageNumber - 1) * pageSize,
            PageSize = pageSize,
            CategoryId = categoryId,
        };

        var products = await connection.QueryAsync<Product>(sql, parameters);

        var countSql = "SELECT COUNT(*) FROM Products WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            countSql += " AND Name LIKE @SearchTerm";
        if (minPrice.HasValue)
            countSql += " AND Price >= @MinPrice";
        if (maxPrice.HasValue)
            countSql += " AND Price <= @MaxPrice";
        if (onlyAvailable == true)
            countSql += " AND Stock > 0";
        if (categoryId.HasValue)
            countSql += " AND CategoryId = @CategoryId";

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<Product>(products.ToList(), totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Products WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task UpdateAsync(Product product)
    {
        const string sql = @"
            UPDATE Products 
            SET Name = @Name, Price = @Price, OriginalPrice = @OriginalPrice, 
                Description = @Description, ImageUrl = @ImageUrl, Stock = @Stock, 
                Rating = @Rating, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Products WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Products WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
