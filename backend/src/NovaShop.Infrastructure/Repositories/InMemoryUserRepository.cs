using NovaShop.Domain.Common;
using NovaShop.Domain.Entities;
using NovaShop.Domain.Repositories;

namespace NovaShop.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new()
    {
        new() { Id = 1, Username = "john_doe", Email = "john@example.com", FirstName = "John", LastName = "Doe", PhoneNumber = "123-456-7890" },
        new() { Id = 2, Username = "jane_smith", Email = "jane@example.com", FirstName = "Jane", LastName = "Smith", PhoneNumber = "098-765-4321" },
        new() { Id = 3, Username = "bob_wilson", Email = "bob@example.com", FirstName = "Bob", LastName = "Wilson", PhoneNumber = "555-1234" },
    };

    public async Task<PagedResult<User>> GetAllAsync(int pageNumber = 1, int pageSize = 12)
    {
        var query = _users.AsQueryable();

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<User>(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return _users.FirstOrDefault(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return _users.FirstOrDefault(u => u.Email == email);
    }

    public async Task<int> AddAsync(User user)
    {
        user.Id = _users.Max(u => u.Id) + 1;
        _users.Add(user);
        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.PhoneNumber = user.PhoneNumber;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
            _users.Remove(user);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return _users.Any(u => u.Id == id);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return _users.Any(u => u.Username == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return _users.Any(u => u.Email == email);
    }
}
