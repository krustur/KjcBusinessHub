using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Infrastructure.PublicHolidays;

/// <summary>
/// Fetches Swedish public holidays (röda dagar) from the Dagsmart API
/// (<c>https://api.dagsmart.se/holidays?year={year}</c>) and upserts them into <see cref="IOffDayRepository"/>.
/// </summary>
public class DagsmartApiPublicHolidayImporter(
    HttpClient httpClient,
    IOffDayRepository repository,
    ILogger<DagsmartApiPublicHolidayImporter> logger) : ISwedishPublicHolidayImporter
{
    public async Task<PublicHolidayImportResult> ImportAsync(int year, CancellationToken cancellationToken = default)
    {
        List<DagsmartHoliday>? days;

        try
        {
            days = await httpClient.GetFromJsonAsync<List<DagsmartHoliday>>(
                $"https://api.dagsmart.se/holidays?year={year}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch public holidays for {Year} from Dagsmart API.", year);
            return new PublicHolidayImportResult(0, 0, $"Could not reach the Dagsmart API: {ex.Message}");
        }

        if (days is null)
        {
            logger.LogWarning("Dagsmart API returned null for year {Year}.", year);
            return new PublicHolidayImportResult(0, 0, "The Dagsmart API returned an empty response.");
        }

        int added = 0, updated = 0;

        foreach (var day in days)
        {
            if (!DateOnly.TryParseExact(day.Date, "yyyy-MM-dd", out var date))
            {
                logger.LogWarning("Could not parse date '{Date}' from Dagsmart API response.", day.Date);
                continue;
            }

            var name = day.Name?.Sv ?? string.Empty;
            var outcome = await repository.UpsertPublicHolidayAsync(year, date, name, cancellationToken);
            switch (outcome)
            {
                case PublicHolidayUpsertOutcome.Inserted:
                    added++;
                    break;
                case PublicHolidayUpsertOutcome.Updated:
                    updated++;
                    break;
            }
        }

        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Imported public holidays for {Year}: {Added} added, {Updated} updated.", year, added, updated);
        return new PublicHolidayImportResult(added, updated, null);
    }

    // ── Dagsmart API response model ──────────────────────────────────────────

    private sealed class DagsmartHoliday
    {
        [JsonPropertyName("date")]
        public string Date { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public DagsmartHolidayName? Name { get; init; }
    }

    private sealed class DagsmartHolidayName
    {
        [JsonPropertyName("sv")]
        public string? Sv { get; init; }

        [JsonPropertyName("en")]
        public string? En { get; init; }
    }
}
