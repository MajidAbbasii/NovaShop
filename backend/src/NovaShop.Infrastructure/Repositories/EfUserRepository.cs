using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;
using NovaShop.Infrastructure.Data;

namespace NovaShop.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly NovaShopDbContext _context;

    public EfUserRepository(NovaShopDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<User>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _context.Users.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Include(u => u.Orders)
            .Include(u => u.Cart)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Orders)
            .Include(u => u.Cart)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Orders)
            .Include(u => u.Cart)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Orders)
            .Include(u => u.Cart)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<int> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
