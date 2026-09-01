using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaShop.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NovaShopDbContext>
{
    public NovaShopDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=NovaShopDb;Username=novashop;Password=novashop-dev;TrustServerCertificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<NovaShopDbContext>();
        optionsBuilder.UseNpgsql(connStr);

        return new NovaShopDbContext(optionsBuilder.Options);
    }
}
