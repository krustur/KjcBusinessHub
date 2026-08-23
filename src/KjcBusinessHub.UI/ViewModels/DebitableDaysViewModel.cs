using System;
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
/// or <see cref="NumberOfMonths"/> change.
/// </summary>
public partial class DebitableDaysViewModel : ViewModelBase
{
    private readonly DebitableDaysCalculator _calculator;

    // ── Start month picker ───────────────────────────────────────────────────

    [ObservableProperty]
    public partial int StartYear { get; set; } = DateOnly.FromDateTime(DateTime.Today).Year;

    [ObservableProperty]
    public partial int StartMonth { get; set; } = 1;

    // ── Number of months ─────────────────────────────────────────────────────

    [ObservableProperty]
    public partial int NumberOfMonths { get; set; } = 12;

    // ── Options ──────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c> (the default), vacation days are deducted from the debitable-days count.
    /// When <c>false</c>, vacation days are treated as ordinary working days.
    /// </summary>
    [ObservableProperty]
    public partial bool DeductVacationDays { get; set; } = true;

    // ── Derived end month ────────────────────────────────────────────────────

    /// <summary>Derived end <see cref="YearMonth"/> based on start + <see cref="NumberOfMonths"/>.</summary>
    public YearMonth EndMonth
    {
        get
        {
            var start = new YearMonth(StartYear, StartMonth);
            var n = Math.Max(1, NumberOfMonths) - 1;
            var totalMonths = start.Month - 1 + n;
            return new YearMonth(start.Year + totalMonths / 12, totalMonths % 12 + 1);
        }
    }

    // ── Results ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    public partial bool IsCalculating { get; set; }

    [ObservableProperty]
    public partial int TotalDebitableDays { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPublicHolidaysWarning))]
    public partial string? NoPublicHolidaysWarning { get; set; }

    public bool HasNoPublicHolidaysWarning => !string.IsNullOrWhiteSpace(NoPublicHolidaysWarning);

    public ObservableCollection<MonthDebitableDaysRow> PerMonth { get; } = [];

    public DebitableDaysViewModel(DebitableDaysCalculator calculator)
    {
        _calculator = calculator;
    }

    // ── Partial property change hooks ────────────────────────────────────────

    partial void OnStartYearChanged(int value) => _ = RecalculateAsync();
    partial void OnStartMonthChanged(int value) => _ = RecalculateAsync();
    partial void OnNumberOfMonthsChanged(int value) => _ = RecalculateAsync();
    partial void OnDeductVacationDaysChanged(bool value) => _ = RecalculateAsync();

    // ── Recalculation ────────────────────────────────────────────────────────

    public async Task RecalculateAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        NoPublicHolidaysWarning = null;
        IsCalculating = true;

        try
        {
            var start = new YearMonth(StartYear, StartMonth);
            var query = new DebitableDaysQuery(start, EndMonth, DeductVacationDays);

            var result = await _calculator.CalculateAsync(query, cancellationToken);

            PerMonth.Clear();
            foreach (var m in result.PerMonth)
            {
                var label = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m.Month.Month)} {m.Month.Year}";
                PerMonth.Add(new MonthDebitableDaysRow(label, m.DebitableDays));
            }

            TotalDebitableDays = result.TotalDebitableDays;

            if (!result.HasPublicHolidays)
            {
                var years = Enumerable.Range(start.Year, EndMonth.Year - start.Year + 1);
                var yearList = string.Join(", ", years);
                NoPublicHolidaysWarning =
                    $"No public holidays found for the selected period. " +
                    $"Consider importing red days for: {yearList}.";
            }
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
