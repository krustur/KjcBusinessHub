namespace KjcBusinessHub.Application.Entities;

/// <summary>
/// Represents a specific year and month combination (no day component).
/// </summary>
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    public YearMonth(DateOnly date) : this(date.Year, date.Month) { }

    /// <summary>Returns the first day of this month.</summary>
    public DateOnly FirstDay() => new(Year, Month, 1);

    /// <summary>Returns the last day of this month.</summary>
    public DateOnly LastDay() => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    /// <summary>Returns the next month.</summary>
    public YearMonth Next() =>
        Month == 12 ? new YearMonth(Year + 1, 1) : new YearMonth(Year, Month + 1);

    public int CompareTo(YearMonth other)
    {
        var yearCmp = Year.CompareTo(other.Year);
        return yearCmp != 0 ? yearCmp : Month.CompareTo(other.Month);
    }

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;
    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;
    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;
    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
