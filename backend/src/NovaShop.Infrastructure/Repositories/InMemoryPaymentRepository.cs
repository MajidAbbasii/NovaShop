using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments = new();

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return _payments.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return _payments.FirstOrDefault(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetByStatusAsync(string status)
    {
        return _payments.Where(p => p.Status == status).ToList();
    }

    public async Task<int> AddAsync(Payment payment)
    {
        payment.Id = _payments.Max(p => (int?)p.Id) + 1 ?? 1;
        _payments.Add(payment);
        return payment.Id;
    }

    public async Task UpdateAsync(Payment payment)
    {
        var existing = _payments.FirstOrDefault(p => p.Id == payment.Id);
        if (existing != null)
        {
            existing.Status = payment.Status;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _payments.Any(p => p.Id == id);
    }
}
