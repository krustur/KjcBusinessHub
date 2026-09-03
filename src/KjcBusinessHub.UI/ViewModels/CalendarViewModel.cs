using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;

namespace KjcBusinessHub.UI.ViewModels;

/// <summary>
/// Represents a single day cell in the mini-calendar grid.
/// </summary>
public class CalendarDayCell : ObservableObject
{
    private bool _isPublicHoliday;
    private bool _isVacation;
    private bool _isBridgingDay;
    private string _publicHolidayDescription = string.Empty;

    /// <summary>The calendar date, or <c>null</c> for empty padding cells.</summary>
    public DateOnly? Date { get; init; }

    /// <summary><c>true</c> when this cell belongs to the displayed month (not a padding cell).</summary>
    public bool IsCurrentMonth { get; init; }

    public bool IsEmpty => !Date.HasValue;

    public int DayNumber => Date?.Day ?? 0;

    public bool IsWeekend =>
        Date.HasValue &&
        (Date.Value.DayOfWeek == DayOfWeek.Saturday || Date.Value.DayOfWeek == DayOfWeek.Sunday);

    public bool IsPublicHoliday
    {
        get => _isPublicHoliday;
        set
        {
            if (SetProperty(ref _isPublicHoliday, value))
            {
                OnVisualStateChanged();
            }
        }
    }

    public bool IsVacation
    {
        get => _isVacation;
        set
        {
            if (SetProperty(ref _isVacation, value))
            {
                OnVisualStateChanged();
            }
        }
    }

    public bool IsBridgingDay
    {
        get => _isBridgingDay;
        set
        {
            if (SetProperty(ref _isBridgingDay, value))
            {
                OnVisualStateChanged();
            }
        }
    }

    public string PublicHolidayDescription
    {
        get => _publicHolidayDescription;
        set
        {
            if (SetProperty(ref _publicHolidayDescription, value))
            {
                OnVisualStateChanged();
            }
        }
    }

    /// <summary>Background brush name for color-coding the cell.</summary>
    public string CellBackground =>
        IsVacation ? "#FFF9C4" :
        IsPublicHoliday ? "#FFCDD2" :
        IsBridgingDay ? "#FFE0B2" :
        IsWeekend ? "#F5F5F5" :
        "Transparent";

    /// <summary>Foreground brush for day number text.</summary>
    public string ForegroundBrush =>
        !IsCurrentMonth ? "#BDBDBD" :
        IsPublicHoliday && !IsVacation ? "#C62828" :
        IsBridgingDay && !IsVacation ? "#E65100" :
        "#212121";

    public string BorderBrush =>
        IsVacation && IsPublicHoliday ? "#C62828" :
        IsVacation && IsWeekend ? "#C62828" :
        IsVacation && IsBridgingDay ? "#E65100" :
        "Transparent";

    public Thickness BorderThickness =>
        IsVacation && (IsPublicHoliday || IsBridgingDay || IsWeekend) ? new Thickness(2) : new Thickness(0);

    public string? ToolTipText
    {
        get
        {
            if (IsVacation)
            {
                var parts = new List<string> { "Vacation" };
                if (IsPublicHoliday)
                {
                    parts.Add(string.IsNullOrWhiteSpace(PublicHolidayDescription)
                        ? "public holiday"
                        : $"public holiday: {PublicHolidayDescription}");
                }

                if (IsBridgingDay)
                    parts.Add("bridging day");

                if (IsWeekend)
                    parts.Add("weekend");

                return string.Join(" + ", parts);
            }

            if (IsPublicHoliday)
                return string.IsNullOrWhiteSpace(PublicHolidayDescription) ? "Public holiday" : PublicHolidayDescription;

            if (IsBridgingDay)
                return "Bridging day";

            return null;
        }
    }

    private void OnVisualStateChanged()
    {
        OnPropertyChanged(nameof(CellBackground));
        OnPropertyChanged(nameof(ForegroundBrush));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(BorderThickness));
        OnPropertyChanged(nameof(ToolTipText));
    }
}

/// <summary>
/// Represents a single month in the year calendar — 6 rows × 7 day cells (Mon–Sun).
/// </summary>
public class MonthCalendarModel
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public string MonthLabel => $"{MonthName} {Year}";

    /// <summary>Up to 6 week-rows; each row contains 7 <see cref="CalendarDayCell"/> objects (Mon–Sun).</summary>
    public IReadOnlyList<IReadOnlyList<CalendarDayCell>> WeekRows { get; init; } = [];
}

public sealed record FiscalYearStartOption(int Year, int Month)
{
    public string DisplayName => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)} {Year}";
    public override string ToString() => DisplayName;
}

/// <summary>
/// ViewModel for the Calendar view — year navigation, off-day display, vacation day toggle, and holiday import.
/// </summary>
public partial class CalendarViewModel : ViewModelBase
{
    private readonly IOffDayRepository _offDayRepository;
    private readonly ISwedishPublicHolidayImporter _importer;

    // Lookup of loaded off-days keyed by date, refreshed on every year load.
    private Dictionary<DateOnly, OffDay> _offDaysByDate = [];

    /// <summary>Action invoked when the user navigates back to the Transactions view.</summary>
    public Action? NavigateToApp { get; set; }

    /// <summary>The embedded Debitable Days panel view model.</summary>
    public DebitableDaysViewModel DebitableDays { get; }

    public ObservableCollection<FiscalYearStartOption> AvailableFiscalYearStarts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImportButtonLabel))]
    public partial FiscalYearStartOption? SelectedFiscalYearStart { get; set; }

    partial void OnSelectedFiscalYearStartChanged(FiscalYearStartOption? value)
    {
        if (value is null) return;
        DebitableDays.ApplyFiscalYearStart(value.Year, value.Month);
        _ = LoadAsync();
    }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string? StatusMessage { get; set; }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string ImportButtonLabel =>
        SelectedFiscalYearStart is null
            ? "Import red days"
            : $"Import red days for {SelectedFiscalYearStart.DisplayName} fiscal year";

    public ObservableCollection<MonthCalendarModel> Months { get; } = [];

    public CalendarViewModel(
        IOffDayRepository offDayRepository,
        ISwedishPublicHolidayImporter importer,
        DebitableDaysViewModel debitableDays)
    {
        _offDayRepository = offDayRepository;
        _importer = importer;
        DebitableDays = debitableDays;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var initialStart = new FiscalYearStartOption(today.Year, DebitableDays.StartMonth);
        RebuildFiscalYearStartOptions(initialStart.Year);
        SelectedFiscalYearStart = FindFiscalYearStartOption(initialStart.Year, initialStart.Month);
    }

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void PreviousYear()
    {
        if (SelectedFiscalYearStart is null) return;
        var targetYear = SelectedFiscalYearStart.Year - 1;
        EnsureFiscalYearStartOptionsInclude(targetYear);
        SelectedFiscalYearStart = FindFiscalYearStartOption(targetYear, SelectedFiscalYearStart.Month);
    }

    [RelayCommand]
    private void NextYear()
    {
        if (SelectedFiscalYearStart is null) return;
        var targetYear = SelectedFiscalYearStart.Year + 1;
        EnsureFiscalYearStartOptionsInclude(targetYear);
        SelectedFiscalYearStart = FindFiscalYearStartOption(targetYear, SelectedFiscalYearStart.Month);
    }

    [RelayCommand]
    private void GoToApp()
    {
        NavigateToApp?.Invoke();
    }

    [RelayCommand]
    private async Task ImportRedDaysAsync(CancellationToken cancellationToken)
    {
        if (IsLoading) return;
        IsLoading = true;
        StatusMessage = null;

        try
        {
            var years = GetRelevantYears();
            var added = 0;
            var updated = 0;
            var errors = new List<string>();

            foreach (var year in years)
            {
                var result = await _importer.ImportAsync(year, cancellationToken);
                if (result.IsSuccess)
                {
                    added += result.Added;
                    updated += result.Updated;
                }
                else
                {
                    errors.Add($"{year}: {result.ErrorMessage}");
                }
            }

            if (errors.Count == 0)
            {
                await LoadAsync(cancellationToken);
                StatusMessage = $"Import complete ({string.Join(", ", years)}): {added} added, {updated} updated.";
            }
            else
            {
                StatusMessage = $"Import failed: {string.Join(" | ", errors)}";
            }
        }
        catch (OperationCanceledException)
        {
            // silently cancelled
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles a date as a vacation day.
    /// Clicking a regular, bridging, or public-holiday day adds/removes the vacation flag without removing
    /// any existing public-holiday metadata for that date.
    /// </summary>
    [RelayCommand]
    private async Task ToggleDayAsync(DateOnly date)
    {
        if (_offDaysByDate.TryGetValue(date, out var existing))
        {
            if (existing.IsVacation)
            {
                if (existing.IsPublicHoliday)
                {
                    existing.IsVacation = false;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await _offDayRepository.UpdateAsync(existing);
                    await _offDayRepository.SaveChangesAsync();
                    ApplyOffDayToCell(date, existing);
                }
                else
                {
                    await _offDayRepository.DeleteAsync(existing.Id);
                    await _offDayRepository.SaveChangesAsync();
                    _offDaysByDate.Remove(date);
                    ApplyOffDayToCell(date, null);
                }
            }
            else
            {
                existing.IsVacation = true;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _offDayRepository.UpdateAsync(existing);
                await _offDayRepository.SaveChangesAsync();
                ApplyOffDayToCell(date, existing);
            }
        }
        else
        {
            var offDay = new OffDay
            {
                Id = Guid.NewGuid(),
                Year = date.Year,
                Date = date,
                IsPublicHoliday = false,
                PublicHolidayDescription = string.Empty,
                IsVacation = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            await _offDayRepository.AddAsync(offDay);
            await _offDayRepository.SaveChangesAsync();
            _offDaysByDate[date] = offDay;
            ApplyOffDayToCell(date, offDay);
        }

        _ = DebitableDays.RecalculateAsync();
    }

    // ── Initialization / loading ─────────────────────────────────────────────

    /// <summary>Loads off-days for the selected fiscal-year period and rebuilds the 12-month calendar grid.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedFiscalYearStart is null) return;

        IsLoading = true;
        StatusMessage = null;
        try
        {
            var years = GetRelevantYears();
            var offDayTasks = years.Select(y => _offDayRepository.GetByYearAsync(y, cancellationToken));
            var offDaysByYear = await Task.WhenAll(offDayTasks);
            var offDays = offDaysByYear.SelectMany(x => x).ToList();
            _offDaysByDate = offDays.ToDictionary(d => d.Date);

            var months = BuildMonthModels(SelectedFiscalYearStart.Year, SelectedFiscalYearStart.Month, _offDaysByDate);

            Months.Clear();
            foreach (var m in months)
                Months.Add(m);

            _ = DebitableDays.RecalculateAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // silently cancelled
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load calendar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IReadOnlyList<int> GetRelevantYears()
    {
        if (SelectedFiscalYearStart is null) return [];

        var periodStart = new DateOnly(SelectedFiscalYearStart.Year, SelectedFiscalYearStart.Month, 1);
        var periodEnd = periodStart.AddMonths(12).AddDays(-1);
        return Enumerable.Range(periodStart.Year, periodEnd.Year - periodStart.Year + 1).ToList();
    }

    private void EnsureFiscalYearStartOptionsInclude(int year)
    {
        var hasYear = AvailableFiscalYearStarts.Any(o => o.Year == year);
        if (!hasYear)
        {
            RebuildFiscalYearStartOptions(year);
        }
    }

    private void RebuildFiscalYearStartOptions(int centerYear)
    {
        AvailableFiscalYearStarts.Clear();
        for (var year = centerYear - 10; year <= centerYear + 10; year++)
        {
            for (var month = 1; month <= 12; month++)
            {
                AvailableFiscalYearStarts.Add(new FiscalYearStartOption(year, month));
            }
        }
    }

    private FiscalYearStartOption? FindFiscalYearStartOption(int year, int month)
    {
        return AvailableFiscalYearStarts.FirstOrDefault(o => o.Year == year && o.Month == month);
    }

    private static IReadOnlyList<MonthCalendarModel> BuildMonthModels(
        int startYear,
        int startMonth,
        IReadOnlyDictionary<DateOnly, OffDay> offDaysByDate)
    {
        var periodStart = new DateOnly(startYear, startMonth, 1);
        var periodEnd = periodStart.AddMonths(12).AddDays(-1);

        var publicHolidays = offDaysByDate.Values
            .Where(d => d.IsPublicHoliday)
            .Where(d => d.Date >= periodStart && d.Date <= periodEnd)
            .Select(d => d.Date)
            .ToHashSet();

        var bridgingDays = DebitableDaysCalculator.ComputeBridgingDays(publicHolidays, periodStart, periodEnd);

        var models = new List<MonthCalendarModel>(12);
        for (var i = 0; i < 12; i++)
        {
            var monthStart = periodStart.AddMonths(i);
            var month = monthStart.Month;
            var year = monthStart.Year;
            var cells = BuildCellsForMonth(year, month, offDaysByDate, bridgingDays);
            var weeks = SplitIntoWeeks(cells);
            models.Add(new MonthCalendarModel
            {
                Year = year,
                Month = month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                WeekRows = weeks,
            });
        }
        return models;
    }

    /// <summary>
    /// Builds 42 calendar cells (6 weeks × 7 days, Mon–Sun) for a given month.
    /// Cells before the first day and after the last day of the month are empty padding cells.
    /// </summary>
    public static IReadOnlyList<CalendarDayCell> BuildCellsForMonth(
        int year,
        int month,
        IReadOnlyDictionary<DateOnly, OffDay>? offDaysByDate = null,
        IReadOnlySet<DateOnly>? bridgingDays = null)
    {
        var cells = new List<CalendarDayCell>(42);

        var firstDay = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // DayOfWeek: Sunday = 0, Monday = 1, …, Saturday = 6
        // We want Mon = 0, …, Sun = 6
        var startPadding = ((int)firstDay.DayOfWeek + 6) % 7; // shift so Monday = 0

        // Leading empty cells
        for (var i = 0; i < startPadding; i++)
            cells.Add(new CalendarDayCell { IsCurrentMonth = false });

        // Days of month
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            OffDay? offDay = null;
            offDaysByDate?.TryGetValue(date, out offDay);

            cells.Add(new CalendarDayCell
            {
                Date = date,
                IsCurrentMonth = true,
                IsPublicHoliday = offDay?.IsPublicHoliday ?? false,
                IsVacation = offDay?.IsVacation ?? false,
                IsBridgingDay = bridgingDays?.Contains(date) ?? false,
                PublicHolidayDescription = offDay?.PublicHolidayDescription ?? string.Empty,
            });
        }

        // Trailing empty cells to complete 42 slots
        while (cells.Count < 42)
            cells.Add(new CalendarDayCell { IsCurrentMonth = false });

        return cells;
    }

    private static IReadOnlyList<IReadOnlyList<CalendarDayCell>> SplitIntoWeeks(
        IReadOnlyList<CalendarDayCell> cells)
    {
        var weeks = new List<IReadOnlyList<CalendarDayCell>>(6);
        for (var i = 0; i < 42; i += 7)
            weeks.Add(cells.Skip(i).Take(7).ToList());
        return weeks;
    }

    /// <summary>
    /// Finds the cell for <paramref name="date"/> in the current <see cref="Months"/> collection
    /// and updates its stored public-holiday/vacation state while preserving the derived bridging-day state.
    /// </summary>
    private void ApplyOffDayToCell(DateOnly date, OffDay? offDay)
    {
        var month = Months.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
        if (month is null) return;

        foreach (var week in month.WeekRows)
        {
            var cell = week.FirstOrDefault(c => c.Date == date);
            if (cell is null) continue;
            cell.IsPublicHoliday = offDay?.IsPublicHoliday ?? false;
            cell.IsVacation = offDay?.IsVacation ?? false;
            cell.PublicHolidayDescription = offDay?.PublicHolidayDescription ?? string.Empty;
            return;
        }
    }
}
