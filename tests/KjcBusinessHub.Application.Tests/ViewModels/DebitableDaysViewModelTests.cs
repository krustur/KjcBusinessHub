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
    public void EndMonth_equals_start_when_NumberOfMonths_is_1()
    {
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 6;
        sut.NumberOfMonths = 1;

        Assert.Equal(new YearMonth(2025, 6), sut.EndMonth);
    }

    [Fact]
    public void EndMonth_crosses_year_boundary()
    {
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 11;
        sut.NumberOfMonths = 3;

        Assert.Equal(new YearMonth(2026, 1), sut.EndMonth);
    }

    // ── RecalculateAsync on property change ──────────────────────────────────

    [Fact]
    public async Task ChangeNumberOfMonths_triggers_recalculation()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();
        sut.StartYear = 2025;
        sut.StartMonth = 1;
        sut.NumberOfMonths = 3;

        await sut.RecalculateAsync();
        Assert.Equal(3, sut.PerMonth.Count);

        SetupEmptyRepo(2025);
        sut.NumberOfMonths = 4;
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
        sut.NumberOfMonths = 1;

        await sut.RecalculateAsync();

        Assert.Single(sut.PerMonth);
        Assert.Equal(23, sut.TotalDebitableDays); // Jan 2025 = 23 weekdays
    }

    [Fact]
    public async Task RecalculateAsync_clears_error_on_valid_input()
    {
        SetupEmptyRepo(2025);
        var sut = CreateSubject();

        // Simulate a calculation error by breaking the repo temporarily
        _repo.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<OffDay>>>(_ => throw new InvalidOperationException("test error"));

        sut.StartYear = 2025;
        sut.StartMonth = 1;
        sut.NumberOfMonths = 1;
        await sut.RecalculateAsync();
        Assert.True(sut.HasError);

        // Fix the repo and recalculate
        SetupEmptyRepo(2025);
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
        sut.NumberOfMonths = 2;

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
        sut.NumberOfMonths = 1;

        await sut.RecalculateAsync();

        Assert.Single(sut.PerMonth);
        Assert.Contains("2025", sut.PerMonth[0].MonthLabel);
    }
}
