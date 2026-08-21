namespace KjcBusinessHub.Application.Interfaces;

/// <summary>Result returned after importing Swedish public holidays for a year.</summary>
public record PublicHolidayImportResult(int Added, int Updated, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
}

public interface ISwedishPublicHolidayImporter
{
    /// <summary>
    /// Fetches Swedish public holidays (röda dagar) for the given year from an external source,
    /// persists them as <c>PublicHoliday</c> off-days, and returns a summary.
    /// Existing <c>Vacation</c> entries are never modified.
    /// </summary>
    Task<PublicHolidayImportResult> ImportAsync(int year, CancellationToken cancellationToken = default);
}
