using KjcBusinessHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KjcBusinessHub.Infrastructure;

/// <summary>
/// Enables EF Core CLI tools to create a DbContext at design time without running the application.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=kjcbusinesshub.db")
            .Options;

        return new AppDbContext(options);
    }
}
