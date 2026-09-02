using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.Application.Services;

/// <summary>
/// Calculates the number of debitable (billable) days in a given period,
/// excluding weekends, public holidays, and optionally vacation and bridging days.
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
        var years = Enumerable.Range(query.StartMonth.Year, query.EndMonth.Year - query.StartMonth.Year + 1).ToList();
        var offDayTasks = years.Select(y => offDayRepository.GetByYearAsync(y, cancellationToken));
        var offDaysByYear = await Task.WhenAll(offDayTasks);

        var allOffDays = offDaysByYear.SelectMany(list => list).ToList();

        var periodStart = query.StartMonth.FirstDay();
        var periodEnd = query.EndMonth.LastDay();

        // Determine which years in the period have no public holidays.
        var yearsWithoutHolidays = years
            .Where(y =>
            {
                var yStart = new DateOnly(y, 1, 1);
                var yEnd = new DateOnly(y, 12, 31);
                var from = yStart > periodStart ? yStart : periodStart;
                var to = yEnd < periodEnd ? yEnd : periodEnd;
                return !allOffDays.Any(d =>
                    d.OffDayType is OffDayType.PublicHoliday &&
                    d.Date >= from &&
                    d.Date <= to);
            })
            .ToList();

        var publicHolidaySet = allOffDays
            .Where(d => d.OffDayType is OffDayType.PublicHoliday)
            .Select(d => d.Date)
            .ToHashSet();

        var bridgingDaySet = ComputeBridgingDays(publicHolidaySet, periodStart, periodEnd);
        var vacationDayCount = allOffDays.Count(d =>
            d.OffDayType is OffDayType.Vacation &&
            d.Date >= periodStart &&
            d.Date <= periodEnd);

        var offDaySet = allOffDays
            .Where(d => d.OffDayType is OffDayType.PublicHoliday
                        || (query.DeductVacationDays && d.OffDayType is OffDayType.Vacation))
            .Select(d => d.Date)
            .ToHashSet();

        if (query.DeductBridgingDays)
        {
            foreach (var bd in bridgingDaySet)
                offDaySet.Add(bd);
        }

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
            YearsWithoutPublicHolidays: yearsWithoutHolidays,
            VacationDayCount: vacationDayCount,
            BridgingDayCount: bridgingDaySet.Count);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes bridging days within [<paramref name="periodStart"/>, <paramref name="periodEnd"/>].
    /// A bridging day is a weekday that is sandwiched between two non-working days
    /// (weekend or public holiday) on both sides, and is not itself a public holiday.
    /// </summary>
    public static HashSet<DateOnly> ComputeBridgingDays(
        HashSet<DateOnly> publicHolidays,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var result = new HashSet<DateOnly>();

        for (var date = periodStart; date <= periodEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (publicHolidays.Contains(date)) continue;

            var prev = date.AddDays(-1);
            var next = date.AddDays(1);

            var prevNonWorking = prev.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                              || publicHolidays.Contains(prev);
            var nextNonWorking = next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                              || publicHolidays.Contains(next);

            if (prevNonWorking && nextNonWorking)
                result.Add(date);
        }

        return result;
    }

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
