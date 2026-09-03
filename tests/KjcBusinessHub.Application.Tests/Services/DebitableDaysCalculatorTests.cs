using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using NSubstitute;

namespace KjcBusinessHub.Application.Tests.Services;

public class DebitableDaysCalculatorTests
{
    private readonly IOffDayRepository _repo = Substitute.For<IOffDayRepository>();

    private DebitableDaysCalculator CreateSubject() => new(_repo);

    // ── Single-month period ──────────────────────────────────────────────────

    [Fact]
    public async Task SingleMonth_no_off_days_counts_only_weekdays()
    {
        // January 2025: 31 days, 23 weekdays
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 1)));

        Assert.Single(result.PerMonth);
        Assert.Equal(23, result.TotalDebitableDays);
        Assert.Equal(23, result.PerMonth[0].DebitableDays);
    }

    [Fact]
    public async Task SingleMonth_public_holiday_reduces_count()
    {
        // Jan 1 2025 is Wednesday — removing it reduces 23 → 22
        var holiday = MakePublicHoliday(new DateOnly(2025, 1, 1));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 1)));

        Assert.Equal(22, result.TotalDebitableDays);
    }

    [Fact]
    public async Task SingleMonth_vacation_day_reduces_count()
    {
        // June 10 2025 is a Tuesday — removing it reduces the June weekday count
        var vacation = MakeVacation(new DateOnly(2025, 6, 10));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 6)));

        // June 2025 has 21 weekdays; minus vacation → 20
        Assert.Equal(20, result.TotalDebitableDays);
    }

    // ── All days off ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AllWeekdaysOff_returns_zero()
    {
        // February 2025 has 20 weekdays
        var offDays = new List<OffDay>();
        var date = new DateOnly(2025, 2, 1);
        while (date.Month == 2)
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                offDays.Add(MakeVacation(date));
            date = date.AddDays(1);
        }

        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>(offDays));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 2), new YearMonth(2025, 2)));

        Assert.Equal(0, result.TotalDebitableDays);
    }

    // ── Multi-month same year ────────────────────────────────────────────────

    [Fact]
    public async Task MultiMonth_same_year_sums_correctly()
    {
        // Jan 2025 = 23 weekdays, Feb 2025 = 20 weekdays
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 2)));

        Assert.Equal(2, result.PerMonth.Count);
        Assert.Equal(23, result.PerMonth[0].DebitableDays);
        Assert.Equal(20, result.PerMonth[1].DebitableDays);
        Assert.Equal(43, result.TotalDebitableDays);
    }

    // ── Multi-year period ────────────────────────────────────────────────────

    [Fact]
    public async Task MultiYear_period_loads_each_year_and_sums()
    {
        // Dec 2024 = 22 weekdays, Jan 2025 = 23 weekdays
        _repo.GetByYearAsync(2024, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2024, 12), new YearMonth(2025, 1)));

        Assert.Equal(2, result.PerMonth.Count);
        Assert.Equal(new YearMonth(2024, 12), result.PerMonth[0].Month);
        Assert.Equal(new YearMonth(2025, 1), result.PerMonth[1].Month);
        Assert.Equal(22 + 23, result.TotalDebitableDays);

        await _repo.Received(1).GetByYearAsync(2024, Arg.Any<CancellationToken>());
        await _repo.Received(1).GetByYearAsync(2025, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MultiYear_off_days_in_both_years_are_excluded()
    {
        var holiday2024 = MakePublicHoliday(new DateOnly(2024, 12, 25)); // Wednesday
        var vacation2025 = MakeVacation(new DateOnly(2025, 1, 2));       // Thursday

        _repo.GetByYearAsync(2024, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday2024]));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation2025]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2024, 12), new YearMonth(2025, 1)));

        Assert.Equal((22 - 1) + (23 - 1), result.TotalDebitableDays);
    }

    // ── DeductVacationDays flag ──────────────────────────────────────────────

    [Fact]
    public async Task DeductVacationDays_false_does_not_subtract_vacation_days()
    {
        var vacation = MakeVacation(new DateOnly(2025, 6, 10));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 6), deductVacationDays: false));

        // June 2025 has 21 weekdays; vacation not deducted → still 21
        Assert.Equal(21, result.TotalDebitableDays);
    }

    [Fact]
    public async Task DeductVacationDays_true_still_subtracts_vacation_days()
    {
        var vacation = MakeVacation(new DateOnly(2025, 6, 10));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 6), deductVacationDays: true));

        Assert.Equal(20, result.TotalDebitableDays);
    }

    // ── HasPublicHolidays flag ───────────────────────────────────────────────

    [Fact]
    public async Task YearsWithoutPublicHolidays_contains_year_when_no_public_holidays_in_period()
    {
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 1)));

        Assert.Contains(2025, result.YearsWithoutPublicHolidays);
    }

    [Fact]
    public async Task YearsWithoutPublicHolidays_is_empty_when_at_least_one_public_holiday_exists()
    {
        var holiday = MakePublicHoliday(new DateOnly(2025, 1, 1));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 1)));

        Assert.Empty(result.YearsWithoutPublicHolidays);
    }

    [Fact]
    public async Task YearsWithoutPublicHolidays_contains_year_when_only_vacation_days_exist()
    {
        var vacation = MakeVacation(new DateOnly(2025, 6, 10));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 6)));

        Assert.Contains(2025, result.YearsWithoutPublicHolidays);
    }

    [Fact]
    public async Task YearsWithoutPublicHolidays_contains_year_when_holiday_exists_outside_queried_period()
    {
        // Holiday in January, but we're querying June
        var holidayOutsidePeriod = MakePublicHoliday(new DateOnly(2025, 1, 1));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holidayOutsidePeriod]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 6)));

        Assert.Contains(2025, result.YearsWithoutPublicHolidays);
    }

    [Fact]
    public async Task YearsWithoutPublicHolidays_only_contains_years_missing_holidays_in_multi_year_period()
    {
        // 2024 has a holiday, 2025 does not
        var holiday2024 = MakePublicHoliday(new DateOnly(2024, 12, 25));
        _repo.GetByYearAsync(2024, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday2024]));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2024, 12), new YearMonth(2025, 1)));

        Assert.DoesNotContain(2024, result.YearsWithoutPublicHolidays);
        Assert.Contains(2025, result.YearsWithoutPublicHolidays);
    }

    // ── Bridging days ────────────────────────────────────────────────────────

    [Fact]
    public void ComputeBridgingDays_detects_friday_between_thursday_holiday_and_weekend()
    {
        // May 1 2025 is a Thursday. May 2 (Friday) should be a bridging day.
        var holidays = new HashSet<DateOnly> { new DateOnly(2025, 5, 1) };
        var start = new DateOnly(2025, 5, 1);
        var end = new DateOnly(2025, 5, 31);

        var result = DebitableDaysCalculator.ComputeBridgingDays(holidays, start, end);

        Assert.Contains(new DateOnly(2025, 5, 2), result);
    }

    [Fact]
    public void ComputeBridgingDays_detects_monday_between_weekend_and_tuesday_holiday()
    {
        // If June 3 2025 (Tuesday) were a holiday, June 2 (Monday) is sandwiched between Sunday and Tuesday.
        var holidays = new HashSet<DateOnly> { new DateOnly(2025, 6, 3) };
        var start = new DateOnly(2025, 6, 1);
        var end = new DateOnly(2025, 6, 30);

        var result = DebitableDaysCalculator.ComputeBridgingDays(holidays, start, end);

        Assert.Contains(new DateOnly(2025, 6, 2), result);
    }

    [Fact]
    public void ComputeBridgingDays_does_not_include_the_holiday_itself()
    {
        var holidays = new HashSet<DateOnly> { new DateOnly(2025, 5, 1) };
        var start = new DateOnly(2025, 5, 1);
        var end = new DateOnly(2025, 5, 31);

        var result = DebitableDaysCalculator.ComputeBridgingDays(holidays, start, end);

        Assert.DoesNotContain(new DateOnly(2025, 5, 1), result);
    }

    [Fact]
    public void ComputeBridgingDays_returns_empty_when_no_holidays()
    {
        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 31);

        var result = DebitableDaysCalculator.ComputeBridgingDays([], start, end);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Bridging_days_do_not_reduce_debitable_days()
    {
        var holiday = MakePublicHoliday(new DateOnly(2025, 5, 1));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 5), new YearMonth(2025, 5)));

        // May 2025: 22 weekdays minus May 1 holiday = 21; May 2 bridging day stays billable.
        Assert.Equal(21, result.TotalDebitableDays);
    }

    [Fact]
    public async Task Public_holiday_and_vacation_combo_only_deducts_once()
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

        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([offDay]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2025, 1), new YearMonth(2025, 1)));

        Assert.Equal(22, result.TotalDebitableDays);
        Assert.Equal(1, result.VacationDayCount);
    }

    // ── DebitableDaysQuery validation ────────────────────────────────────────

    [Fact]
    public void Query_end_before_start_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DebitableDaysQuery(new YearMonth(2025, 6), new YearMonth(2025, 5)));
    }

    [Fact]
    public void Query_same_month_is_valid()
    {
        var query = new DebitableDaysQuery(new YearMonth(2025, 3), new YearMonth(2025, 3));
        Assert.Equal(new YearMonth(2025, 3), query.StartMonth);
        Assert.Equal(new YearMonth(2025, 3), query.EndMonth);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static OffDay MakePublicHoliday(DateOnly date, string description = "Holiday") =>
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
