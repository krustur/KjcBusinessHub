using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Infrastructure.PublicHolidays;

/// <summary>
/// Fetches Swedish public holidays (röda dagar) from the free Dagar API
/// (<c>https://api.dagar.se/v1/{year}</c>) and upserts them into <see cref="IOffDayRepository"/>.
/// </summary>
public class DagarApiPublicHolidayImporter(
    HttpClient httpClient,
    IOffDayRepository repository,
    ILogger<DagarApiPublicHolidayImporter> logger) : ISwedishPublicHolidayImporter
{
    public async Task<PublicHolidayImportResult> ImportAsync(int year, CancellationToken cancellationToken = default)
    {
        List<DagarDay>? days;

        try
        {
            days = await httpClient.GetFromJsonAsync<List<DagarDay>>(
                $"https://api.dagar.se/v1/{year}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch public holidays for {Year} from Dagar API.", year);
            return new PublicHolidayImportResult(0, 0, $"Could not reach the Dagar API: {ex.Message}");
        }

        if (days is null)
        {
            logger.LogWarning("Dagar API returned null for year {Year}.", year);
            return new PublicHolidayImportResult(0, 0, "The Dagar API returned an empty response.");
        }

        int added = 0, updated = 0;

        foreach (var day in days.Where(d => d.RedDay))
        {
            if (!DateOnly.TryParseExact(day.Date, "yyyy-MM-dd", out var date))
            {
                logger.LogWarning("Could not parse date '{Date}' from Dagar API response.", day.Date);
                continue;
            }

            var isNew = await repository.UpsertPublicHolidayAsync(year, date, day.Name ?? string.Empty, cancellationToken);
            if (isNew) added++; else updated++;
        }

        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Imported public holidays for {Year}: {Added} added, {Updated} updated.", year, added, updated);
        return new PublicHolidayImportResult(added, updated, null);
    }

    // ── Dagar API response model ─────────────────────────────────────────────

    private sealed class DagarDay
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("red_day")]
        public bool RedDay { get; init; }
    }
}
