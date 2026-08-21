using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Tests.Entities;

public class CalendarYearTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_with_valid_off_days_succeeds()
    {
        var offDays = new[]
        {
            MakeOffDay(new DateOnly(2025, 1, 1), OffDayType.PublicHoliday),
            MakeOffDay(new DateOnly(2025, 6, 6), OffDayType.PublicHoliday),
        };

        var year = new CalendarYear(2025, offDays);

        Assert.Equal(2025, year.Year);
        Assert.Equal(2, year.OffDays.Count);
    }

    [Fact]
    public void Constructor_with_off_day_wrong_year_throws()
    {
        var wrongYear = MakeOffDay(new DateOnly(2024, 12, 31), OffDayType.PublicHoliday);

        Assert.Throws<ArgumentException>(() => new CalendarYear(2025, [wrongYear]));
    }

    [Fact]
    public void Constructor_with_duplicate_dates_throws()
    {
        var a = MakeOffDay(new DateOnly(2025, 1, 1), OffDayType.PublicHoliday);
        var b = MakeOffDay(new DateOnly(2025, 1, 1), OffDayType.Vacation);

        Assert.Throws<ArgumentException>(() => new CalendarYear(2025, [a, b]));
    }

    // ── AddOffDay ────────────────────────────────────────────────────────────

    [Fact]
    public void AddOffDay_valid_day_increases_count()
    {
        var year = new CalendarYear(2025);
        var offDay = MakeOffDay(new DateOnly(2025, 7, 14), OffDayType.Vacation);

        year.AddOffDay(offDay);

        Assert.Single(year.OffDays);
    }

    [Fact]
    public void AddOffDay_wrong_year_throws()
    {
        var year = new CalendarYear(2025);
        var offDay = MakeOffDay(new DateOnly(2026, 1, 1), OffDayType.Vacation);

        Assert.Throws<ArgumentException>(() => year.AddOffDay(offDay));
    }

    [Fact]
    public void AddOffDay_duplicate_date_throws()
    {
        var year = new CalendarYear(2025);
        var first = MakeOffDay(new DateOnly(2025, 7, 14), OffDayType.PublicHoliday);
        var second = MakeOffDay(new DateOnly(2025, 7, 14), OffDayType.Vacation);

        year.AddOffDay(first);

        Assert.Throws<ArgumentException>(() => year.AddOffDay(second));
    }

    // ── RemoveOffDay ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveOffDay_existing_id_removes_and_returns_true()
    {
        var offDay = MakeOffDay(new DateOnly(2025, 7, 14), OffDayType.Vacation);
        var year = new CalendarYear(2025, [offDay]);

        var removed = year.RemoveOffDay(offDay.Id);

        Assert.True(removed);
        Assert.Empty(year.OffDays);
    }

    [Fact]
    public void RemoveOffDay_unknown_id_returns_false()
    {
        var year = new CalendarYear(2025);

        var removed = year.RemoveOffDay(Guid.NewGuid());

        Assert.False(removed);
    }

    // ── FindByDate ───────────────────────────────────────────────────────────

    [Fact]
    public void FindByDate_returns_matching_entry()
    {
        var date = new DateOnly(2025, 6, 6);
        var offDay = MakeOffDay(date, OffDayType.PublicHoliday);
        var year = new CalendarYear(2025, [offDay]);

        var found = year.FindByDate(date);

        Assert.NotNull(found);
        Assert.Equal(offDay.Id, found.Id);
    }

    [Fact]
    public void FindByDate_no_match_returns_null()
    {
        var year = new CalendarYear(2025);

        var found = year.FindByDate(new DateOnly(2025, 6, 6));

        Assert.Null(found);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static OffDay MakeOffDay(DateOnly date, OffDayType type) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = date.Year,
            Date = date,
            OffDayType = type,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
