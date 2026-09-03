using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Application.Services;
using KjcBusinessHub.UI.ViewModels;
using NSubstitute;

namespace KjcBusinessHub.Application.Tests.ViewModels;

public class CalendarViewModelTests
{
    private readonly IOffDayRepository _offDayRepository = Substitute.For<IOffDayRepository>();
    private readonly ISwedishPublicHolidayImporter _importer = Substitute.For<ISwedishPublicHolidayImporter>();
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();

    private CalendarViewModel CreateSubject()
    {
        _settings.FiscalStartMonth.Returns(1);
        var debitableDays = new DebitableDaysViewModel(new DebitableDaysCalculator(_offDayRepository), _settings);
        return new CalendarViewModel(_offDayRepository, _importer, debitableDays);
    }

    private static FiscalYearStartOption FindFiscalYearStart(CalendarViewModel sut, int year, int month) =>
        sut.AvailableFiscalYearStarts.Single(o => o.Year == year && o.Month == month);

    // ── Year navigation ──────────────────────────────────────────────────────

    [Fact]
    public void PreviousYearCommand_decrements_selected_year()
    {
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
        var sut = CreateSubject();
        var initial = sut.SelectedFiscalYearStart!.Year;

        sut.PreviousYearCommand.Execute(null);

        Assert.Equal(initial - 1, sut.SelectedFiscalYearStart!.Year);
    }

    [Fact]
    public void NextYearCommand_increments_selected_year()
    {
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
        var sut = CreateSubject();
        var initial = sut.SelectedFiscalYearStart!.Year;

        sut.NextYearCommand.Execute(null);

        Assert.Equal(initial + 1, sut.SelectedFiscalYearStart!.Year);
    }

    // ── LoadAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_populates_12_months()
    {
        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        Assert.Equal(12, sut.Months.Count);
    }

    [Fact]
    public async Task LoadAsync_shows_months_for_selected_fiscal_year_period()
    {
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 6);

        await sut.LoadAsync();

        Assert.Equal(12, sut.Months.Count);
        Assert.Equal((2025, 6), (sut.Months[0].Year, sut.Months[0].Month));
        Assert.Equal((2026, 5), (sut.Months[11].Year, sut.Months[11].Month));
        Assert.Equal($"{sut.Months[0].MonthName} {sut.Months[0].Year}", sut.Months[0].MonthLabel);
    }

    [Fact]
    public async Task LoadAsync_applies_public_holiday_to_correct_cell()
    {
        var holiday = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 12, 25),
            IsPublicHoliday = true,
            PublicHolidayDescription = "Christmas Day",
            IsVacation = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _offDayRepository.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        // December is month 12 → index 11
        var december = sut.Months[11];
        CalendarDayCell? cell = null;
        foreach (var week in december.WeekRows)
            foreach (var c in week)
                if (c.Date == new DateOnly(2025, 12, 25))
                    cell = c;

        Assert.NotNull(cell);
        Assert.True(cell.IsPublicHoliday);
        Assert.Equal("Christmas Day", cell.PublicHolidayDescription);
    }

    [Fact]
    public async Task LoadAsync_sets_status_message_on_exception()
    {
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<OffDay>>>(_ => throw new InvalidOperationException("DB down"));

        var sut = CreateSubject();
        await sut.LoadAsync();

        Assert.NotNull(sut.StatusMessage);
        Assert.Contains("DB down", sut.StatusMessage);
    }

    // ── Day toggle ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleDayCommand_adds_vacation_for_regular_day()
    {
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));
        _offDayRepository.AddAsync(Arg.Any<OffDay>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _offDayRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        var targetDate = new DateOnly(2025, 6, 10); // regular Tuesday
        await sut.ToggleDayCommand.ExecuteAsync(targetDate);

        await _offDayRepository.Received(1).AddAsync(
            Arg.Is<OffDay>(d => d.Date == targetDate && d.IsVacation && !d.IsPublicHoliday),
            Arg.Any<CancellationToken>());
        await _offDayRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleDayCommand_removes_existing_vacation_day()
    {
        var vacation = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 7, 14),
            IsPublicHoliday = false,
            PublicHolidayDescription = string.Empty,
            IsVacation = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _offDayRepository.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([vacation]));
        _offDayRepository.DeleteAsync(vacation.Id, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _offDayRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        await sut.ToggleDayCommand.ExecuteAsync(vacation.Date);

        await _offDayRepository.Received(1).DeleteAsync(vacation.Id, Arg.Any<CancellationToken>());
        await _offDayRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleDayCommand_adds_vacation_flag_to_public_holiday()
    {
        var holiday = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 1),
            IsPublicHoliday = true,
            PublicHolidayDescription = "New Year's Day",
            IsVacation = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _offDayRepository.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));
        _offDayRepository.UpdateAsync(Arg.Any<OffDay>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _offDayRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        await sut.ToggleDayCommand.ExecuteAsync(holiday.Date);

        await _offDayRepository.Received(1).UpdateAsync(
            Arg.Is<OffDay>(d => d.Id == holiday.Id && d.IsPublicHoliday && d.IsVacation && d.PublicHolidayDescription == "New Year's Day"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleDayCommand_removes_only_vacation_flag_from_public_holiday_combo()
    {
        var holiday = new OffDay
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            Date = new DateOnly(2025, 1, 1),
            IsPublicHoliday = true,
            PublicHolidayDescription = "New Year's Day",
            IsVacation = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _offDayRepository.GetByYearAsync(2025, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([holiday]));
        _offDayRepository.UpdateAsync(Arg.Any<OffDay>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _offDayRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 1);
        await sut.LoadAsync();

        await sut.ToggleDayCommand.ExecuteAsync(holiday.Date);

        await _offDayRepository.Received(1).UpdateAsync(
            Arg.Is<OffDay>(d => d.Id == holiday.Id && d.IsPublicHoliday && !d.IsVacation && d.PublicHolidayDescription == "New Year's Day"),
            Arg.Any<CancellationToken>());
        await _offDayRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void BuildCellsForMonth_marks_bridging_day_plus_vacation_combo_with_vacation_priority()
    {
        var date = new DateOnly(2025, 5, 2);
        var cells = CalendarViewModel.BuildCellsForMonth(
            2025,
            5,
            new Dictionary<DateOnly, OffDay>
            {
                [date] = new()
                {
                    Id = Guid.NewGuid(),
                    Year = 2025,
                    Date = date,
                    IsVacation = true,
                    IsPublicHoliday = false,
                    PublicHolidayDescription = string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            },
            new HashSet<DateOnly> { date });

        var cell = Assert.Single(cells, c => c.Date == date);

        Assert.True(cell.IsVacation);
        Assert.True(cell.IsBridgingDay);
        Assert.False(cell.IsPublicHoliday);
        Assert.Equal("#FFF9C4", cell.CellBackground);
        Assert.Equal("#E65100", cell.BorderBrush);
    }

    [Fact]
    public void BuildCellsForMonth_marks_public_holiday_plus_vacation_combo_with_vacation_priority()
    {
        var date = new DateOnly(2025, 1, 1);
        var cells = CalendarViewModel.BuildCellsForMonth(
            2025,
            1,
            new Dictionary<DateOnly, OffDay>
            {
                [date] = new()
                {
                    Id = Guid.NewGuid(),
                    Year = 2025,
                    Date = date,
                    IsVacation = true,
                    IsPublicHoliday = true,
                    PublicHolidayDescription = "New Year's Day",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            });

        var cell = Assert.Single(cells, c => c.Date == date);

        Assert.True(cell.IsVacation);
        Assert.True(cell.IsPublicHoliday);
        Assert.Equal("#FFF9C4", cell.CellBackground);
        Assert.Equal("#C62828", cell.BorderBrush);
        Assert.Contains("public holiday", cell.ToolTipText);
    }

    [Fact]
    public void BuildCellsForMonth_marks_weekend_plus_vacation_combo_with_public_holiday_error_style()
    {
        var date = new DateOnly(2025, 1, 4);
        var cells = CalendarViewModel.BuildCellsForMonth(
            2025,
            1,
            new Dictionary<DateOnly, OffDay>
            {
                [date] = new()
                {
                    Id = Guid.NewGuid(),
                    Year = 2025,
                    Date = date,
                    IsVacation = true,
                    IsPublicHoliday = false,
                    PublicHolidayDescription = string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            });

        var cell = Assert.Single(cells, c => c.Date == date);

        Assert.True(cell.IsVacation);
        Assert.True(cell.IsWeekend);
        Assert.Equal("#FFF9C4", cell.CellBackground);
        Assert.Equal("#C62828", cell.BorderBrush);
        Assert.Equal(new Avalonia.Thickness(2), cell.BorderThickness);
        Assert.Equal("Vacation + weekend", cell.ToolTipText);
    }

    // ── Import red days ──────────────────────────────────────────────────────

    [Fact]
    public async Task ImportRedDaysCommand_shows_success_summary_on_success()
    {
        _importer.ImportAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PublicHolidayImportResult(Added: 12, Updated: 1, ErrorMessage: null));
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        await sut.ImportRedDaysCommand.ExecuteAsync(null);

        Assert.NotNull(sut.StatusMessage);
        Assert.Contains("12", sut.StatusMessage);
        Assert.Contains("1", sut.StatusMessage);
    }

    [Fact]
    public async Task ImportRedDaysCommand_shows_error_message_on_failure()
    {
        _importer.ImportAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PublicHolidayImportResult(Added: 0, Updated: 0, ErrorMessage: "Network unreachable"));

        var sut = CreateSubject();
        await sut.ImportRedDaysCommand.ExecuteAsync(null);

        Assert.NotNull(sut.StatusMessage);
        Assert.Contains("Network unreachable", sut.StatusMessage);
    }

    [Fact]
    public async Task ImportRedDaysCommand_reloads_calendar_on_success()
    {
        _importer.ImportAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PublicHolidayImportResult(Added: 5, Updated: 0, ErrorMessage: null));
        _offDayRepository.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OffDay>>([]));

        var sut = CreateSubject();
        sut.SelectedFiscalYearStart = FindFiscalYearStart(sut, 2025, 6);
        await sut.ImportRedDaysCommand.ExecuteAsync(null);

        await _importer.Received().ImportAsync(2025, Arg.Any<CancellationToken>());
        await _importer.Received().ImportAsync(2026, Arg.Any<CancellationToken>());
        await _offDayRepository.Received().GetByYearAsync(2025, Arg.Any<CancellationToken>());
        await _offDayRepository.Received().GetByYearAsync(2026, Arg.Any<CancellationToken>());
    }

    // ── GoToApp navigation ───────────────────────────────────────────────────

    [Fact]
    public void GoToAppCommand_invokes_NavigateToApp_action()
    {
        var invoked = false;
        var sut = CreateSubject();
        sut.NavigateToApp = () => invoked = true;

        sut.GoToAppCommand.Execute(null);

        Assert.True(invoked);
    }

    // ── BuildCellsForMonth helper ─────────────────────────────────────────────

    [Theory]
    [InlineData(2024, 1)]   // January 2024 starts on Monday
    [InlineData(2024, 3)]   // March 2024 starts on Friday
    [InlineData(2025, 6)]   // June 2025 starts on Sunday
    public void BuildCellsForMonth_returns_42_cells(int year, int month)
    {
        var cells = CalendarViewModel.BuildCellsForMonth(year, month);
        Assert.Equal(42, cells.Count);
    }

    [Theory]
    [InlineData(2024, 1, 31)]
    [InlineData(2024, 2, 29)]   // leap year
    [InlineData(2025, 2, 28)]
    [InlineData(2024, 3, 31)]
    public void BuildCellsForMonth_contains_correct_number_of_current_month_cells(int year, int month, int expectedDays)
    {
        var cells = CalendarViewModel.BuildCellsForMonth(year, month);
        var currentMonthCells = 0;
        foreach (var c in cells)
            if (c.IsCurrentMonth)
                currentMonthCells++;

        Assert.Equal(expectedDays, currentMonthCells);
    }

    [Fact]
    public void BuildCellsForMonth_marks_saturday_and_sunday_as_weekend()
    {
        // January 6 2024 = Saturday, January 7 2024 = Sunday
        var cells = CalendarViewModel.BuildCellsForMonth(2024, 1);
        CalendarDayCell? saturday = null, sunday = null;
        foreach (var c in cells)
        {
            if (c.Date == new DateOnly(2024, 1, 6)) saturday = c;
            if (c.Date == new DateOnly(2024, 1, 7)) sunday = c;
        }

        Assert.NotNull(saturday);
        Assert.True(saturday.IsWeekend);
        Assert.NotNull(sunday);
        Assert.True(sunday.IsWeekend);
    }

    [Fact]
    public void BuildCellsForMonth_first_day_is_in_correct_Monday_column()
    {
        // January 2025 starts on Wednesday (column index 2 for Mon-based grid)
        var cells = CalendarViewModel.BuildCellsForMonth(2025, 1);

        // First 2 cells should be empty (Monday, Tuesday padding)
        Assert.True(cells[0].IsEmpty);
        Assert.True(cells[1].IsEmpty);
        Assert.Equal(new DateOnly(2025, 1, 1), cells[2].Date);
    }
}
