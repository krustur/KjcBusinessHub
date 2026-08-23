using KjcBusinessHub.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace KjcBusinessHub.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();
    public DbSet<OffDay> OffDays => Set<OffDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
