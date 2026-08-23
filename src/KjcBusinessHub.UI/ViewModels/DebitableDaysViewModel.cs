using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Services;

namespace KjcBusinessHub.UI.ViewModels;

/// <summary>One row in the per-month debitable-days breakdown table.</summary>
public sealed record MonthDebitableDaysRow(string MonthLabel, int DebitableDays);

/// <summary>
/// ViewModel for the Debitable Days panel embedded in the Calendar view.
/// Recalculates whenever <see cref="StartYear"/>, <see cref="StartMonth"/>,
/// <see cref="EndYear"/>, or <see cref="EndMonth"/> change.
/// </summary>
public partial class DebitableDaysViewModel : ViewModelBase
{
    private readonly DebitableDaysCalculator _calculator;

    // ── Start month picker ───────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndBeforeStart))]
    public partial int StartYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndBeforeStart))]
    public partial int StartMonth { get; set; } = 1;

    // ── End month picker ─────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndBeforeStart))]
    public partial int EndYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEndBeforeStart))]
    public partial int EndMonth { get; set; } = 12;

    // ── Results ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    public partial bool IsCalculating { get; set; }

    [ObservableProperty]
    public partial int TotalDebitableDays { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEndBeforeStart =>
        new YearMonth(EndYear, EndMonth) < new YearMonth(StartYear, StartMonth);

    public ObservableCollection<MonthDebitableDaysRow> PerMonth { get; } = [];

    // ── Month name helpers (for combo boxes) ─────────────────────────────────

    public static IReadOnlyList<(int Value, string Label)> MonthItems { get; } =
        Enumerable.Range(1, 12)
            .Select(m => (m, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)))
            .ToList();

    public DebitableDaysViewModel(DebitableDaysCalculator calculator)
    {
        _calculator = calculator;
    }

    // ── Partial property change hooks ────────────────────────────────────────

    partial void OnStartYearChanged(int value) => _ = RecalculateAsync();
    partial void OnStartMonthChanged(int value) => _ = RecalculateAsync();
    partial void OnEndYearChanged(int value) => _ = RecalculateAsync();
    partial void OnEndMonthChanged(int value) => _ = RecalculateAsync();

    // ── Recalculation ────────────────────────────────────────────────────────

    public async Task RecalculateAsync(CancellationToken cancellationToken = default)
    {
        if (IsEndBeforeStart)
        {
            PerMonth.Clear();
            TotalDebitableDays = 0;
            ErrorMessage = "End month must be on or after start month.";
            return;
        }

        ErrorMessage = null;
        IsCalculating = true;

        try
        {
            var query = new DebitableDaysQuery(
                new YearMonth(StartYear, StartMonth),
                new YearMonth(EndYear, EndMonth));

            var result = await _calculator.CalculateAsync(query, cancellationToken);

            PerMonth.Clear();
            foreach (var m in result.PerMonth)
            {
                var label = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m.Month.Month)} {m.Month.Year}";
                PerMonth.Add(new MonthDebitableDaysRow(label, m.DebitableDays));
            }

            TotalDebitableDays = result.TotalDebitableDays;
        }
        catch (OperationCanceledException)
        {
            // silently cancelled
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not calculate debitable days: {ex.Message}";
        }
        finally
        {
            IsCalculating = false;
        }
    }
}
