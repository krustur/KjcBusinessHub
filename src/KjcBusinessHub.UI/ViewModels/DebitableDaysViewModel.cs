using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Services;

namespace KjcBusinessHub.UI.ViewModels;

/// <summary>One row in the per-month debitable-days breakdown table.</summary>
public sealed record MonthDebitableDaysRow(string MonthLabel, int DebitableDays);

/// <summary>One selectable item in the fiscal-year start-month picker.</summary>
public sealed record FiscalStartMonthOption(int Month)
{
    public string DisplayName { get; } =
        CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);

    public override string ToString() => DisplayName;
}

/// <summary>
/// ViewModel for the Debitable Days panel embedded in the Calendar view.
/// The period is always exactly 12 months starting from <see cref="FiscalStartMonth"/>
/// within the year provided by the parent (<see cref="CalendarYear"/>).
/// </summary>
public partial class DebitableDaysViewModel : ViewModelBase
{
    private readonly DebitableDaysCalculator _calculator;

    // ── Calendar year (set by the parent CalendarViewModel) ──────────────────

    private int _calendarYear = DateOnly.FromDateTime(DateTime.Today).Year;

    /// <summary>
    /// The year currently shown in the Calendar view.
    /// Setting this value triggers a recalculation.
    /// </summary>
    public int CalendarYear
    {
        get => _calendarYear;
        set
        {
            if (_calendarYear == value) return;
            _calendarYear = value;
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    // ── Fiscal year start month ──────────────────────────────────────────────

    /// <summary>All twelve months available as dropdown options.</summary>
    public IReadOnlyList<FiscalStartMonthOption> AvailableStartMonths { get; } =
        Enumerable.Range(1, 12)
                  .Select(m => new FiscalStartMonthOption(m))
                  .ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedStartMonth))]
    public partial int StartMonth { get; set; } = 1;

    /// <summary>The currently selected <see cref="FiscalStartMonthOption"/>; drives the dropdown.</summary>
    public FiscalStartMonthOption SelectedStartMonth
    {
        get => AvailableStartMonths[StartMonth - 1];
        set
        {
            if (value is not null)
                StartMonth = value.Month;
        }
    }

    // ── Derived end month ────────────────────────────────────────────────────

    /// <summary>
    /// Derived end <see cref="YearMonth"/>: always 11 months after the start (i.e. a full 12-month year).
    /// </summary>
    public YearMonth EndMonth
    {
        get
        {
            var start = new YearMonth(CalendarYear, StartMonth);
            var totalMonths = start.Month - 1 + 11;
            return new YearMonth(start.Year + totalMonths / 12, totalMonths % 12 + 1);
        }
    }

    // ── Options ──────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c> (the default), vacation days are deducted from the debitable-days count.
    /// When <c>false</c>, vacation days are treated as ordinary working days.
    /// </summary>
    [ObservableProperty]
    public partial bool DeductVacationDays { get; set; } = true;

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

    partial void OnStartMonthChanged(int value) => _ = RecalculateAsync();
    partial void OnDeductVacationDaysChanged(bool value) => _ = RecalculateAsync();

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void PreviousStartMonth()
    {
        StartMonth = StartMonth == 1 ? 12 : StartMonth - 1;
    }

    [RelayCommand]
    private void NextStartMonth()
    {
        StartMonth = StartMonth == 12 ? 1 : StartMonth + 1;
    }

    // ── Recalculation ────────────────────────────────────────────────────────

    public async Task RecalculateAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        NoPublicHolidaysWarning = null;
        IsCalculating = true;

        try
        {
            var start = new YearMonth(CalendarYear, StartMonth);
            var query = new DebitableDaysQuery(start, EndMonth, DeductVacationDays);

            var result = await _calculator.CalculateAsync(query, cancellationToken);

            PerMonth.Clear();
            foreach (var m in result.PerMonth)
            {
                var label = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m.Month.Month)} {m.Month.Year}";
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
