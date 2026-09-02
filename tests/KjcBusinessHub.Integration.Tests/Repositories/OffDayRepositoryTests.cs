using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Infrastructure.Data;
using KjcBusinessHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KjcBusinessHub.Integration.Tests.Repositories;

public class OffDayRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OffDayRepository _repository;

    public OffDayRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repository = new OffDayRepository(_db);
    }

    // ── GetByYearAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByYearAsync_returns_only_entries_for_requested_year()
    {
        await _repository.AddAsync(MakeOffDay(2025, new DateOnly(2025, 1, 1)));
        await _repository.AddAsync(MakeOffDay(2026, new DateOnly(2026, 1, 1)));
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByYearAsync(2025);

        Assert.Single(result);
        Assert.Equal(new DateOnly(2025, 1, 1), result[0].Date);
    }

    [Fact]
    public async Task GetByYearAsync_returns_empty_list_for_year_with_no_entries()
    {
        var result = await _repository.GetByYearAsync(2025);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByYearAsync_returns_entries_ordered_by_date()
    {
        await _repository.AddAsync(MakeOffDay(2025, new DateOnly(2025, 12, 25)));
        await _repository.AddAsync(MakeOffDay(2025, new DateOnly(2025, 1, 1)));
        await _repository.SaveChangesAsync();

        var result = await _repository.GetByYearAsync(2025);

        Assert.Equal(new DateOnly(2025, 1, 1), result[0].Date);
        Assert.Equal(new DateOnly(2025, 12, 25), result[1].Date);
    }

    // ── AddAsync / GetByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_and_GetByIdAsync_round_trips()
    {
        var offDay = MakeOffDay(2025, new DateOnly(2025, 6, 6), OffDayType.PublicHoliday, "Sveriges nationaldag");
        await _repository.AddAsync(offDay);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(offDay.Id);

        Assert.NotNull(found);
        Assert.Equal(offDay.Id, found.Id);
        Assert.Equal(OffDayType.PublicHoliday, found.OffDayType);
        Assert.Equal("Sveriges nationaldag", found.Description);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_persists_changed_description()
    {
        var offDay = MakeOffDay(2025, new DateOnly(2025, 6, 6));
        await _repository.AddAsync(offDay);
        await _repository.SaveChangesAsync();

        offDay.Description = "Updated description";
        await _repository.UpdateAsync(offDay);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(offDay.Id);
        Assert.Equal("Updated description", found!.Description);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_removes_entry()
    {
        var offDay = MakeOffDay(2025, new DateOnly(2025, 6, 6));
        await _repository.AddAsync(offDay);
        await _repository.SaveChangesAsync();

        await _repository.DeleteAsync(offDay.Id);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(offDay.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.DeleteAsync(Guid.NewGuid()));
    }

    // ── UpsertPublicHolidayAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpsertPublicHolidayAsync_inserts_new_entry_and_returns_inserted()
    {
        var outcome = await _repository.UpsertPublicHolidayAsync(2025, new DateOnly(2025, 1, 1), "Nyårsdagen");
        await _repository.SaveChangesAsync();

        Assert.Equal(PublicHolidayUpsertOutcome.Inserted, outcome);
        var all = await _repository.GetByYearAsync(2025);
        Assert.Single(all);
        Assert.Equal(OffDayType.PublicHoliday, all[0].OffDayType);
    }

    [Fact]
    public async Task UpsertPublicHolidayAsync_updates_existing_public_holiday_and_returns_updated()
    {
        await _repository.UpsertPublicHolidayAsync(2025, new DateOnly(2025, 1, 1), "Old name");
        await _repository.SaveChangesAsync();

        var outcome = await _repository.UpsertPublicHolidayAsync(2025, new DateOnly(2025, 1, 1), "New name");
        await _repository.SaveChangesAsync();

        Assert.Equal(PublicHolidayUpsertOutcome.Updated, outcome);
        var all = await _repository.GetByYearAsync(2025);
        Assert.Single(all);
        Assert.Equal("New name", all[0].Description);
    }

    [Fact]
    public async Task UpsertPublicHolidayAsync_leaves_vacation_entry_untouched_and_returns_skipped()
    {
        var vacation = MakeOffDay(2025, new DateOnly(2025, 1, 1), OffDayType.Vacation, "Summer vacation");
        await _repository.AddAsync(vacation);
        await _repository.SaveChangesAsync();

        var outcome = await _repository.UpsertPublicHolidayAsync(2025, new DateOnly(2025, 1, 1), "Nyårsdagen");
        await _repository.SaveChangesAsync();

        Assert.Equal(PublicHolidayUpsertOutcome.Skipped, outcome);
        var all = await _repository.GetByYearAsync(2025);
        Assert.Single(all);
        Assert.Equal(OffDayType.Vacation, all[0].OffDayType);
        Assert.Equal("Summer vacation", all[0].Description);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static OffDay MakeOffDay(
        int year, DateOnly date,
        OffDayType type = OffDayType.PublicHoliday,
        string description = "") =>
        new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Date = date,
            OffDayType = type,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Dispose() => _db.Dispose();
}
