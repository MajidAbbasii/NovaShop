using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Auth;
using NovaShop.Domain.Entities;

namespace NovaShop.Infrastructure.Data;

public class NovaShopDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductColor> ProductColors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<SmsNotification> SmsNotifications { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<AppNotification> AppNotifications { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<CustomDollRequest> CustomDollRequests { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public NovaShopDbContext(DbContextOptions<NovaShopDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product - Category
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        // Concurrency token (optimistic locking) for inventory safety.
        modelBuilder.Entity<Product>()
            .Property(p => p.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // ProductImage - Product
        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductImage - ProductColor (nullable: product-level images)
        // NO ACTION: deleting a color cascades via Product → Colors → (implicit),
        // and Product → Images also cascades; another cascade path would cycle.
        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.ProductColor)
            .WithMany(c => c.Images)
            .HasForeignKey(pi => pi.ProductColorId)
            .OnDelete(DeleteBehavior.NoAction);

        // ProductColor - Product
        modelBuilder.Entity<ProductColor>()
            .HasOne(pc => pc.Product)
            .WithMany(p => p.Colors)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order - User
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId);

        // OrderItem - Order
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId);

        // OrderItem - Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId);

        // Review - Product
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId);

        // Review - User
        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);

        // Cart - User
        modelBuilder.Entity<Cart>()
            .HasOne(c => c.User)
            .WithOne(u => u.Cart)
            .HasForeignKey<Cart>(c => c.UserId);

        // Payment - Order
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId);

        // CartItem - Cart
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId);

        // CartItem - Product
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId);

        // WishlistItem - User
        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId);

        // WishlistItem - Product
        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.ProductId);

        modelBuilder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.ProductId })
            .IsUnique();

        // Discount (owned collection fields)
        modelBuilder.Entity<Discount>(d =>
        {
            d.Property(x => x.Code).IsRequired().HasMaxLength(50);
            d.HasIndex(x => x.Code).IsUnique();
            d.Property(x => x.Value).HasColumnType("decimal(18,2)");
            d.Property(x => x.MinOrderAmount).HasColumnType("decimal(18,2)");
            d.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            d.Ignore(x => x.ApplicableProductIds);
            d.Ignore(x => x.ApplicableCategoryIds);
        });

        // Order - Discount
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Discount)
            .WithMany()
            .HasForeignKey(o => o.DiscountId)
            .OnDelete(DeleteBehavior.SetNull);

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(h =>
        {
            h.HasOne(x => x.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            h.Property(x => x.FromStatus).HasMaxLength(30);
            h.Property(x => x.ToStatus).HasMaxLength(30);
            h.Property(x => x.Note).HasMaxLength(500);
            h.HasIndex(x => x.OrderId);
        });

        // InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(t =>
        {
            t.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
            t.Property(x => x.Type).HasMaxLength(20);
            t.Property(x => x.Reference).HasMaxLength(100);
            t.HasIndex(x => x.ProductId);
            t.HasIndex(x => x.OrderId);
        });

        // SmsNotification
        modelBuilder.Entity<SmsNotification>(n =>
        {
            n.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
            n.Property(x => x.PhoneNumber).HasMaxLength(20);
            n.Property(x => x.EventType).HasMaxLength(50);
            n.Property(x => x.Provider).HasMaxLength(30);
            n.Property(x => x.Status).HasMaxLength(20);
            n.Property(x => x.Message).HasMaxLength(1000);
            n.Property(x => x.ProviderMessageId).HasMaxLength(100);
            n.Property(x => x.Error).HasMaxLength(500);
            n.HasIndex(x => x.OrderId);
        });

        // User - PhoneNumber uniqueness (mobile-first auth requires one account per mobile)
        modelBuilder.Entity<User>(u =>
        {
            u.Property(x => x.PhoneNumber).HasMaxLength(20);
            u.Property(x => x.Username).HasMaxLength(50);
            u.Property(x => x.Email).HasMaxLength(100);
            u.Property(x => x.FirstName).HasMaxLength(100);
            u.Property(x => x.LastName).HasMaxLength(100);
            u.Property(x => x.Address).HasMaxLength(500);
            u.Property(x => x.City).HasMaxLength(100);
            u.Property(x => x.PostalCode).HasMaxLength(20);
            u.HasIndex(x => x.PhoneNumber).IsUnique();
            u.HasIndex(x => x.Username).IsUnique();
        });

        // Wallet - User (one-to-one)
        modelBuilder.Entity<Wallet>()
            .HasOne(w => w.User)
            .WithOne()
            .HasForeignKey<Wallet>(w => w.UserId);

        // WalletTransaction
        modelBuilder.Entity<WalletTransaction>(t =>
        {
            t.HasOne(x => x.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
            t.Property(x => x.Type).HasMaxLength(20);
            t.Property(x => x.Status).HasMaxLength(20);
            t.Property(x => x.Description).HasMaxLength(500);
            t.Property(x => x.Reference).HasMaxLength(100);
            t.Property(x => x.FailureReason).HasMaxLength(500);
            t.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            t.Property(x => x.BalanceBefore).HasColumnType("decimal(18,2)");
            t.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
            t.HasIndex(x => x.WalletId);
            t.HasIndex(x => x.OrderId);
            t.HasIndex(x => new { x.WalletId, x.Type });
        });

        // AppNotification
        modelBuilder.Entity<AppNotification>(n =>
        {
            n.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            n.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
            n.HasOne(x => x.CustomDollRequest)
                .WithMany()
                .HasForeignKey(x => x.CustomDollRequestId)
                .OnDelete(DeleteBehavior.NoAction);
            n.Property(x => x.Type).HasMaxLength(50);
            n.Property(x => x.Channel).HasMaxLength(20);
            n.Property(x => x.Title).HasMaxLength(200);
            n.Property(x => x.Message).HasMaxLength(1000);
            n.Property(x => x.Status).HasMaxLength(20);
            n.Property(x => x.Error).HasMaxLength(500);
            n.HasIndex(x => x.UserId);
            n.HasIndex(x => x.OrderId);
            n.HasIndex(x => new { x.UserId, x.IsRead });
        });

        // Order shipping/payment properties
        modelBuilder.Entity<Order>(o =>
        {
            o.Property(x => x.ShippingMethod).HasMaxLength(20);
            o.Property(x => x.PaymentStatus).HasMaxLength(20);
            o.Property(x => x.PaymentMethod).HasMaxLength(30);
            o.Property(x => x.ShippingCost).HasColumnType("decimal(18,2)");
            o.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
            o.HasIndex(x => x.PaymentStatus);
            o.HasIndex(x => x.ShippingMethod);
            o.HasIndex(x => x.Status);
        });

        // CustomDollRequest
        modelBuilder.Entity<CustomDollRequest>(r =>
        {
            r.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            r.Property(x => x.Status).HasMaxLength(20);
            r.Property(x => x.Currency).HasMaxLength(20);
            r.Property(x => x.ImageUrl).HasMaxLength(500);
            r.Property(x => x.Description).HasMaxLength(2000);
            r.Property(x => x.AdminMessage).HasMaxLength(2000);
            r.Property(x => x.Price).HasColumnType("decimal(18,2)");
            r.HasIndex(x => x.UserId);
            r.HasIndex(x => x.Status);
        });

        // RefreshToken - User (one-to-many: a user can have several active refresh tokens)
        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            rt.Property(x => x.Token).IsRequired().HasMaxLength(100);
            rt.HasIndex(x => x.Token).IsUnique();
            rt.HasIndex(x => x.UserId);
        });
    }
}
