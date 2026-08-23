using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.UI.ViewModels;
using NSubstitute;

namespace KjcBusinessHub.Application.Tests.ViewModels;

public class DebitableDaysViewModelTests
{
    private readonly IOffDayRepository _repo = Substitute.For<IOffDayRepository>();

    private DebitableDaysViewModel CreateSubject()
    {
        var calculator = new DebitableDaysCalculator(_repo);
        return new DebitableDaysViewModel(calculator);
    }

    private void SetupEmptyRepo(params int[] years)
    {
        foreach (var y in years)
            _repo.GetByYearAsync(y, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
    }

    // ── EndMonth derivation ──────────────────────────────────────────────────

    [Fact]
    public void EndMonth_is_always_12_months_from_start_within_same_year()
    {
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        // Jan → Dec of the same year
        Assert.Equal(new YearMonth(2025, 12), sut.EndMonth);
    }

    [Fact]
    public void EndMonth_crosses_year_boundary_when_start_month_is_not_January()
    {
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 6;

        // Jun 2025 → May 2026
        Assert.Equal(new YearMonth(2026, 5), sut.EndMonth);
    }

    // ── Always 12 months ─────────────────────────────────────────────────────

    [Fact]
    public async Task RecalculateAsync_always_produces_12_PerMonth_rows()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();

        Assert.Equal(12, sut.PerMonth.Count);
    }

    [Fact]
    public async Task RecalculateAsync_produces_12_rows_spanning_two_years_when_start_not_January()
    {
        SetupEmptyRepo(2025, 2026);
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 6; // Jun 2025 – May 2026

        await sut.RecalculateAsync();

        Assert.Equal(12, sut.PerMonth.Count);
    }

    // ── RecalculateAsync on property change ──────────────────────────────────

    [Fact]
    public async Task ChangeStartMonth_triggers_recalculation()
    {
        SetupEmptyRepo(2025, 2026);
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();
        var firstTotal = sut.TotalDebitableDays;

        sut.StartMonth = 6;
        await sut.RecalculateAsync();

        // Different start month → different set of months → different total
        Assert.NotEqual(firstTotal, sut.TotalDebitableDays);
    }

    [Fact]
    public async Task ChangeCalendarYear_triggers_recalculation()
    {
        SetupEmptyRepo(2024, 2025, 2026);
        var sut = CreateSubject();
        sut.CalendarYear = 2024;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();
        var total2024 = sut.TotalDebitableDays;

        sut.CalendarYear = 2025;
        await sut.RecalculateAsync();

        Assert.NotEqual(total2024, sut.TotalDebitableDays);
    }

    [Fact]
    public async Task RecalculateAsync_clears_error_on_valid_input()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();

        // Simulate a calculation error by breaking the repo temporarily
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<OffDay>>>(_ => throw new InvalidOperationException("test error"));

        sut.CalendarYear = 2025;
        sut.StartMonth = 1;
        await sut.RecalculateAsync();
        Assert.True(sut.HasError);

        // Fix the repo and recalculate
        SetupEmptyRepo(2025);
        await sut.RecalculateAsync();

        Assert.False(sut.HasError);
        Assert.Null(sut.ErrorMessage);
    }

    // ── PerMonth row labels ──────────────────────────────────────────────────

    [Fact]
    public async Task RecalculateAsync_row_label_contains_month_name_and_year()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();

        Assert.Equal(12, sut.PerMonth.Count);
        Assert.Contains("2025", sut.PerMonth[0].MonthLabel); // first row is January 2025
    }

    // ── DeductVacationDays option ────────────────────────────────────────────

    [Fact]
    public async Task DeductVacationDays_false_excludes_vacation_from_total()
    {
        // Jan 6 2025 is a Monday vacation day; CalendarYear=2025, StartMonth=1 spans Jan–Dec 2025
        var vacation = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 6),
            OffDayType = OffDayType.Vacation,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;
        sut.DeductVacationDays = false;

        await sut.RecalculateAsync();

        // Vacation not deducted → all 12 months of 2025 weekdays (261 total)
        Assert.Equal(261, sut.TotalDebitableDays);
    }

    [Fact]
    public async Task DeductVacationDays_true_subtracts_vacation_from_total()
    {
        // Jan 6 2025 is a Monday vacation day
        var vacation = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 6),
            OffDayType = OffDayType.Vacation,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;
        sut.DeductVacationDays = true;

        await sut.RecalculateAsync();

        Assert.Equal(260, sut.TotalDebitableDays);
    }

    [Fact]
    public async Task DeductVacationDays_change_triggers_recalculation()
    {
        var vacation = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 6),
            OffDayType = OffDayType.Vacation,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));

        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        // With deduction (default)
        await sut.RecalculateAsync();
        Assert.Equal(260, sut.TotalDebitableDays);

        // Without deduction
        sut.DeductVacationDays = false;
        await sut.RecalculateAsync();
        Assert.Equal(261, sut.TotalDebitableDays);
    }

    // ── No public holidays warning ───────────────────────────────────────────

    [Fact]
    public async Task RecalculateAsync_sets_warning_when_no_public_holidays()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();

        Assert.True(sut.HasNoPublicHolidaysWarning);
        Assert.NotNull(sut.NoPublicHolidaysWarning);
        Assert.Contains("2025", sut.NoPublicHolidaysWarning);
    }

    [Fact]
    public async Task RecalculateAsync_no_warning_when_public_holidays_exist()
    {
        var holiday = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 1),
            OffDayType = OffDayType.PublicHoliday,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));

        var sut = CreateSubject();
        sut.CalendarYear = 2025;
        sut.StartMonth = 1;

        await sut.RecalculateAsync();

        Assert.False(sut.HasNoPublicHolidaysWarning);
        Assert.Null(sut.NoPublicHolidaysWarning);
    }

    // ── AvailableStartMonths and SelectedStartMonth ──────────────────────────

    [Fact]
    public void AvailableStartMonths_contains_12_options()
    {
        var sut = CreateSubject();
        Assert.Equal(12, sut.AvailableStartMonths.Count);
    }

    [Fact]
    public void SelectedStartMonth_reflects_StartMonth()
    {
        var sut = CreateSubject();
        sut.StartMonth = 6;
        Assert.Equal(6, sut.SelectedStartMonth.Month);
    }

    [Fact]
    public void Setting_SelectedStartMonth_updates_StartMonth()
    {
        var sut = CreateSubject();
        sut.SelectedStartMonth = sut.AvailableStartMonths[8]; // September
        Assert.Equal(9, sut.StartMonth);
    }
}
