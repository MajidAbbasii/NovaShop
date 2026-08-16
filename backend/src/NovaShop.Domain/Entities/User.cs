namespace NovaShop.Domain.Entities;

public class User
{
    public const string RoleAdmin = "Admin";
    public const string RoleCustomer = "Customer";

    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Role { get; set; } = RoleCustomer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Order> Orders { get; private set; } = new();
    public Cart? Cart { get; private set; }
    public List<WishlistItem> WishlistItems { get; private set; } = new();
}
