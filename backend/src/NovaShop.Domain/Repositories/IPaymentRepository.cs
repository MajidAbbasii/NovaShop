using NovaShop.Domain.Entities;

namespace NovaShop.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<List<Payment>> GetByStatusAsync(string status);
    Task<int> AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task<bool> ExistsAsync(int id);
}
