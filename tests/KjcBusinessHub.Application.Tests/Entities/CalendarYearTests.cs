using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Tests.Entities;

public class CalendarYearTests
{
    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_with_valid_off_days_succeeds()
    {
        var offDays = new[]
        {
            MakePublicHoliday(new DateOnly(2025, 1, 1)),
            MakePublicHoliday(new DateOnly(2025, 6, 6)),
        };

        var year = new CalendarYear(2025, offDays);

        Assert.Equal(2025, year.Year);
        Assert.Equal(2, year.OffDays.Count);
    }

    [Fact]
    public void Constructor_with_off_day_wrong_year_throws()
    {
        var wrongYear = MakePublicHoliday(new DateOnly(2024, 12, 31));

        Assert.Throws<ArgumentException>(() => new CalendarYear(2025, [wrongYear]));
    }

    [Fact]
    public void Constructor_with_duplicate_dates_throws()
    {
        var a = MakePublicHoliday(new DateOnly(2025, 1, 1));
        var b = MakeVacation(new DateOnly(2025, 1, 1));

        Assert.Throws<ArgumentException>(() => new CalendarYear(2025, [a, b]));
    }

    // ── AddOffDay ────────────────────────────────────────────────────────────

    [Fact]
    public void AddOffDay_valid_day_increases_count()
    {
        var year = new CalendarYear(2025);
        var offDay = MakeVacation(new DateOnly(2025, 7, 14));

        year.AddOffDay(offDay);

        Assert.Single(year.OffDays);
    }

    [Fact]
    public void AddOffDay_wrong_year_throws()
    {
        var year = new CalendarYear(2025);
        var offDay = MakeVacation(new DateOnly(2026, 1, 1));

        Assert.Throws<ArgumentException>(() => year.AddOffDay(offDay));
    }

    [Fact]
    public void AddOffDay_duplicate_date_throws()
    {
        var year = new CalendarYear(2025);
        var first = MakePublicHoliday(new DateOnly(2025, 7, 14));
        var second = MakeVacation(new DateOnly(2025, 7, 14));

        year.AddOffDay(first);

        Assert.Throws<ArgumentException>(() => year.AddOffDay(second));
    }

    // ── RemoveOffDay ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveOffDay_existing_id_removes_and_returns_true()
    {
        var offDay = MakeVacation(new DateOnly(2025, 7, 14));
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
        var offDay = MakePublicHoliday(date);
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

    [Fact]
    public void Constructor_allows_combined_public_holiday_and_vacation_flags()
    {
        var offDay = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 1),
            IsPublicHoliday = true,
            PublicHolidayDescription = "Nyårsdagen",
            IsVacation = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var year = new CalendarYear(2025, [offDay]);

        Assert.Single(year.OffDays);
        Assert.True(year.OffDays[0].IsPublicHoliday);
        Assert.True(year.OffDays[0].IsVacation);
    }

    [Fact]
    public void AddOffDay_without_any_flag_throws()
    {
        var year = new CalendarYear(2025);
        var offDay = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 7, 14),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Assert.Throws<ArgumentException>(() => year.AddOffDay(offDay));
    }

    [Fact]
    public void AddOffDay_with_public_holiday_description_but_no_public_holiday_flag_throws()
    {
        var year = new CalendarYear(2025);
        var offDay = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 7, 14),
            PublicHolidayDescription = "Invalid",
            IsVacation = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Assert.Throws<ArgumentException>(() => year.AddOffDay(offDay));
    }

    private static OffDay MakePublicHoliday(DateOnly date, string description = "") =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = date.Year,
            Date = date,
            IsPublicHoliday = true,
            PublicHolidayDescription = description,
            IsVacation = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static OffDay MakeVacation(DateOnly date) =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = date.Year,
            Date = date,
            IsPublicHoliday = false,
            PublicHolidayDescription = string.Empty,
            IsVacation = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
