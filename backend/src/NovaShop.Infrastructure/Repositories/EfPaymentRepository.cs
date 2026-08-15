using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfPaymentRepository : IPaymentRepository
{
    private readonly NovaShopDbContext _context;

    public EfPaymentRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetByStatusAsync(string status)
    {
        return await _context.Payments
            .Where(p => p.Status == status)
            .Include(p => p.Order)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment.Id;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Payments.AnyAsync(p => p.Id == id);
    }
}
