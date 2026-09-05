using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using KjcBusinessHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KjcBusinessHub.Infrastructure.Repositories;

public class OffDayRepository(AppDbContext db) : IOffDayRepository
{
    public async Task<IReadOnlyList<OffDay>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
        => await db.OffDays
            .Where(d => d.Year == year)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);

    public async Task<OffDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.OffDays.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(OffDay offDay, CancellationToken cancellationToken = default)
    {
        offDay.Validate();
        await db.OffDays.AddAsync(offDay, cancellationToken);
    }

    public Task UpdateAsync(OffDay offDay, CancellationToken cancellationToken = default)
    {
        offDay.Validate();
        db.OffDays.Update(offDay);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await db.OffDays.SingleOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"OffDay {id} not found.");
        db.OffDays.Remove(existing);
    }

    public async Task<PublicHolidayUpsertOutcome> UpsertPublicHolidayAsync(int year, DateOnly date, string description, CancellationToken cancellationToken = default)
    {
        var existing = await db.OffDays.SingleOrDefaultAsync(
            d => d.Year == year && d.Date == date, cancellationToken);

        if (existing is not null)
        {
            existing.IsPublicHoliday = true;
            existing.PublicHolidayDescription = description;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            db.OffDays.Update(existing);
            return PublicHolidayUpsertOutcome.Updated;
        }

        await db.OffDays.AddAsync(new OffDay
        {
            Id = Guid.NewGuid(),
            Year = year,
            Date = date,
            IsPublicHoliday = true,
            PublicHolidayDescription = description,
            AbsenceType = AbsenceType.None,
            CreatedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        return PublicHolidayUpsertOutcome.Inserted;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);
}
