using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

/// <summary>
/// Aggregate that represents all tracked off-days for a single calendar year.
/// </summary>
public class CalendarYear
{
    private readonly List<OffDay> _offDays;

    public int Year { get; }
    public IReadOnlyList<OffDay> OffDays => _offDays.AsReadOnly();

    public CalendarYear(int year, IEnumerable<OffDay>? offDays = null)
    {
        Year = year;
        _offDays = offDays?.ToList() ?? [];

        foreach (var day in _offDays)
            Validate(day);
    }

    /// <summary>Adds a new off-day to this calendar year.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the date does not belong to this year, no day flags are set, or a conflicting entry already exists for that date.
    /// </exception>
    public void AddOffDay(OffDay offDay)
    {
        Validate(offDay);
        _offDays.Add(offDay);
    }

    /// <summary>Removes an off-day by its id. Returns <c>false</c> if no matching entry is found.</summary>
    public bool RemoveOffDay(Guid id)
    {
        var existing = _offDays.FirstOrDefault(d => d.Id == id);
        if (existing is null)
            return false;

        _offDays.Remove(existing);
        return true;
    }

    /// <summary>Returns the off-day for the given date, or <c>null</c> if none exists.</summary>
    public OffDay? FindByDate(DateOnly date) =>
        _offDays.FirstOrDefault(d => d.Date == date);

    // ── private helpers ──────────────────────────────────────────────────────

    private void Validate(OffDay offDay)
    {
        if (offDay.Date.Year != Year)
            throw new ArgumentException(
                $"OffDay date {offDay.Date} does not belong to year {Year}.", nameof(offDay));

        if (!offDay.IsPublicHoliday && !offDay.IsVacation)
            throw new ArgumentException(
                $"OffDay {offDay.Date} must be either a public holiday or a vacation day.", nameof(offDay));

        if (!offDay.IsPublicHoliday && !string.IsNullOrWhiteSpace(offDay.PublicHolidayDescription))
            throw new ArgumentException(
                $"OffDay {offDay.Date} cannot have a public-holiday description without being a public holiday.", nameof(offDay));

        var conflict = _offDays.FirstOrDefault(d => d.Date == offDay.Date && d.Id != offDay.Id);
        if (conflict is not null)
            throw new ArgumentException(
                $"An off-day entry already exists for {offDay.Date}.", nameof(offDay));
    }
}
