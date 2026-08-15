using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class DapperPaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;

    public DapperPaymentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Payments WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { Id = id });
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        const string sql = "SELECT * FROM Payments WHERE OrderId = @OrderId";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { OrderId = orderId });
    }

    public async Task<List<Payment>> GetByStatusAsync(string status)
    {
        const string sql = "SELECT * FROM Payments WHERE Status = @Status";
        using var connection = new SqlConnection(_connectionString);
        var payments = await connection.QueryAsync<Payment>(sql, new { Status = status });
        return payments.ToList();
    }

    public async Task<int> AddAsync(Payment payment)
    {
        const string sql = @"
            INSERT INTO Payments (OrderId, Amount, PaymentMethod, Status, TransactionId, CreatedAt)
            VALUES (@OrderId, @Amount, @PaymentMethod, @Status, @TransactionId, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

        using var connection = new SqlConnection(_connectionString);
        var id = await connection.ExecuteScalarAsync<int>(sql, payment);
        payment.Id = id;
        return id;
    }

    public async Task UpdateAsync(Payment payment)
    {
        const string sql = @"
            UPDATE Payments 
            SET Status = @Status
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, payment);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        const string sql = "SELECT COUNT(*) FROM Payments WHERE Id = @Id";
        using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
