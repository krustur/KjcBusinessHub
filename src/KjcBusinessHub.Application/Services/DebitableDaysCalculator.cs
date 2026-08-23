using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.Application.Services;

/// <summary>
/// Calculates the number of debitable (billable) days in a given period,
/// excluding weekends, public holidays, and vacation days.
/// </summary>
public class DebitableDaysCalculator(IOffDayRepository offDayRepository)
{
    /// <summary>
    /// Calculates debitable days for the period defined by <paramref name="query"/>.
    /// Off-day data is loaded from the repository for each year that the period spans.
    /// </summary>
    public async Task<DebitableDaysResult> CalculateAsync(
        DebitableDaysQuery query,
        CancellationToken cancellationToken = default)
    {
        // Collect all years spanned by the query and load off-days for each.
        var years = Enumerable.Range(query.StartMonth.Year, query.EndMonth.Year - query.StartMonth.Year + 1);
        var offDayTasks = years.Select(y => offDayRepository.GetByYearAsync(y, cancellationToken));
        var offDaysByYear = await Task.WhenAll(offDayTasks);

        var allOffDays = offDaysByYear.SelectMany(list => list).ToList();

        var periodStart = query.StartMonth.FirstDay();
        var periodEnd = query.EndMonth.LastDay();

        var hasPublicHolidays = allOffDays.Any(d =>
            d.OffDayType is OffDayType.PublicHoliday &&
            d.Date >= periodStart &&
            d.Date <= periodEnd);

        var offDaySet = allOffDays
            .Where(d => d.OffDayType is OffDayType.PublicHoliday
                        || (query.DeductVacationDays && d.OffDayType is OffDayType.Vacation))
            .Select(d => d.Date)
            .ToHashSet();

        var perMonth = new List<MonthDebitableDays>();
        var current = query.StartMonth;

        while (current <= query.EndMonth)
        {
            var count = CountDebitableDaysInMonth(current, offDaySet);
            perMonth.Add(new MonthDebitableDays(current, count));
            current = current.Next();
        }

        return new DebitableDaysResult(
            TotalDebitableDays: perMonth.Sum(m => m.DebitableDays),
            PerMonth: perMonth,
            HasPublicHolidays: hasPublicHolidays);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static int CountDebitableDaysInMonth(YearMonth month, HashSet<DateOnly> offDays)
    {
        var count = 0;
        var first = month.FirstDay();
        var last = month.LastDay();

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (offDays.Contains(date)) continue;
            count++;
        }

        return count;
    }
}
