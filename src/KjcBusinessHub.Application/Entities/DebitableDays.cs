namespace KjcBusinessHub.Application.Entities;

/// <summary>
/// Debitable-day count for a single month within a <see cref="DebitableDaysResult"/>.
/// </summary>
public sealed record MonthDebitableDays(YearMonth Month, int DebitableDays);

/// <summary>
/// Result of a debitable-days calculation for a given period.
/// </summary>
public sealed record DebitableDaysResult(
    int TotalDebitableDays,
    IReadOnlyList<MonthDebitableDays> PerMonth,
    IReadOnlyList<int> YearsWithoutPublicHolidays,
    int AbsenceDayCount);

/// <summary>
/// Specifies the period for a debitable-days calculation.
/// </summary>
public sealed record DebitableDaysQuery
{
    /// <summary>First month of the period (inclusive).</summary>
    public YearMonth StartMonth { get; init; }

    /// <summary>Last month of the period (inclusive); must be ≥ <see cref="StartMonth"/>.</summary>
    public YearMonth EndMonth { get; init; }

    public DebitableDaysQuery(YearMonth startMonth, YearMonth endMonth, bool deductAbsenceDays = true)
    {
        if (endMonth < startMonth)
            throw new ArgumentException(
                $"EndMonth ({endMonth}) must be greater than or equal to StartMonth ({startMonth}).",
                nameof(endMonth));

        StartMonth = startMonth;
        EndMonth = endMonth;
        DeductAbsenceDays = deductAbsenceDays;
    }

    /// <summary>
    /// When <c>true</c> (the default), absence days are deducted from the debitable-days count.
    /// When <c>false</c>, absences are treated as ordinary working days and not deducted.
    /// </summary>
    public bool DeductAbsenceDays { get; init; }
}
