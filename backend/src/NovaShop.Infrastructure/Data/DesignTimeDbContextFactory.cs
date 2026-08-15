using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaShop.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NovaShopDbContext>
{
    public NovaShopDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NovaShopDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NovaShopDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new NovaShopDbContext(optionsBuilder.Options);
    }
}
