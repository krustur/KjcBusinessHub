using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
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
        var holiday = MakeOffDay(new DateOnly(2025, 1, 1), OffDayType.PublicHoliday);
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
        var vacation = MakeOffDay(new DateOnly(2025, 6, 10), OffDayType.Vacation);
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
                offDays.Add(MakeOffDay(date, OffDayType.Vacation));
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
        var holiday2024 = MakeOffDay(new DateOnly(2024, 12, 25), OffDayType.PublicHoliday); // Wednesday
        var vacation2025 = MakeOffDay(new DateOnly(2025, 1, 2), OffDayType.Vacation);       // Thursday

        _repo.GetByYearAsync(2024, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday2024]));
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation2025]));

        var sut = CreateSubject();
        var result = await sut.CalculateAsync(
            new DebitableDaysQuery(new YearMonth(2024, 12), new YearMonth(2025, 1)));

        Assert.Equal((22 - 1) + (23 - 1), result.TotalDebitableDays);
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
