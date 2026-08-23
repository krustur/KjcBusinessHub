using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.Application.Validators;
using KjcBusinessHub.Infrastructure.Data;
using KjcBusinessHub.Infrastructure.FileSystem;
using KjcBusinessHub.Infrastructure.PublicHolidays;
using KjcBusinessHub.Infrastructure.Repositories;
using KjcBusinessHub.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KjcBusinessHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string settingsFilePath)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ISourceDocumentRepository, SourceDocumentRepository>();
        services.AddScoped<IOffDayRepository, OffDayRepository>();
        services.AddHttpClient<ISwedishPublicHolidayImporter, DagsmartApiPublicHolidayImporter>();
        services.AddTransient<SourceDocumentValidator>();
        services.AddSingleton<ISettingsService>(serviceProvider =>
            new SettingsService(
                settingsFilePath,
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SettingsService>>()));
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddTransient<TransactionImportService>();
        services.AddTransient<SourceDocumentImportService>();
        services.AddTransient<DebitableDaysCalculator>();
        services.AddSingleton<FileWatcherService>();

        return services;
    }

    /// <summary>Ensure the database is created and all pending migrations are applied.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
