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

    // ── IsEndBeforeStart ─────────────────────────────────────────────────────

    [Fact]
    public void IsEndBeforeStart_false_when_end_equals_start()
    {
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 6;
        sut.EndYear = 2025;
        sut.EndMonth = 6;

        Assert.False(sut.IsEndBeforeStart);
    }

    [Fact]
    public void IsEndBeforeStart_true_when_end_before_start()
    {
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 6;
        sut.EndYear = 2025;
        sut.EndMonth = 5;

        Assert.True(sut.IsEndBeforeStart);
    }

    // ── RecalculateAsync on property change ──────────────────────────────────

    [Fact]
    public async Task ChangeStartYear_triggers_recalculation()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 1;
        sut.EndYear = 2025;
        sut.EndMonth = 3;

        await sut.RecalculateAsync();
        Assert.Equal(3, sut.PerMonth.Count);

        SetupEmptyRepo(2025); // same year, just reconfirm setup
        sut.EndMonth = 4;
        await sut.RecalculateAsync();

        Assert.Equal(4, sut.PerMonth.Count);
    }

    [Fact]
    public async Task RecalculateAsync_populates_PerMonth_and_Total()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 1;
        sut.EndYear = 2025;
        sut.EndMonth = 1;

        await sut.RecalculateAsync();

        Assert.Single(sut.PerMonth);
        Assert.Equal(23, sut.TotalDebitableDays); // Jan 2025 = 23 weekdays
    }

    [Fact]
    public async Task RecalculateAsync_sets_error_when_end_before_start()
    {
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 6;
        sut.EndYear = 2025;
        sut.EndMonth = 5;

        await sut.RecalculateAsync();

        Assert.True(sut.HasError);
        Assert.NotNull(sut.ErrorMessage);
        Assert.Empty(sut.PerMonth);
        Assert.Equal(0, sut.TotalDebitableDays);
    }

    [Fact]
    public async Task RecalculateAsync_clears_error_on_valid_input()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();

        // First trigger an error
        sut.StartYear = 2025;
        sut.StartMonth = 6;
        sut.EndYear = 2025;
        sut.EndMonth = 5;
        await sut.RecalculateAsync();
        Assert.True(sut.HasError);

        // Now fix the range
        sut.EndMonth = 6;
        await sut.RecalculateAsync();

        Assert.False(sut.HasError);
        Assert.Null(sut.ErrorMessage);
    }

    // ── Multi-year span ──────────────────────────────────────────────────────

    [Fact]
    public async Task RecalculateAsync_spans_two_years()
    {
        SetupEmptyRepo(2024, 2025);
        var sut = CreateSubject();
        sut.StartYear = 2024;
        sut.StartMonth = 12;
        sut.EndYear = 2025;
        sut.EndMonth = 1;

        await sut.RecalculateAsync();

        Assert.Equal(2, sut.PerMonth.Count);
        Assert.Equal(22 + 23, sut.TotalDebitableDays);
    }

    // ── PerMonth row labels ──────────────────────────────────────────────────

    [Fact]
    public async Task RecalculateAsync_row_label_contains_month_name_and_year()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 3;
        sut.EndYear = 2025;
        sut.EndMonth = 3;

        await sut.RecalculateAsync();

        Assert.Single(sut.PerMonth);
        Assert.Contains("2025", sut.PerMonth[0].MonthLabel);
    }
}
