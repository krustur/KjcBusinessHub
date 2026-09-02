using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Interfaces;

public enum PublicHolidayUpsertOutcome
{
    Inserted,
    Updated,
    Skipped,
}

public interface IOffDayRepository
{
    /// <summary>Returns all off-days for the given year.</summary>
    Task<IReadOnlyList<OffDay>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>Returns the off-day with the given id, or <c>null</c> if not found.</summary>
    Task<OffDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new off-day entry.</summary>
    Task AddAsync(OffDay offDay, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing off-day entry.</summary>
    Task UpdateAsync(OffDay offDay, CancellationToken cancellationToken = default);

    /// <summary>Deletes an off-day entry by its id.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a public-holiday off-day for the given date.
    /// If an entry with the same date already exists and is a <see cref="OffDayType.PublicHoliday"/>,
    /// its description is updated. Vacation entries are left untouched.
    /// Returns whether the public holiday was inserted, updated, or skipped.
    /// </summary>
    Task<PublicHolidayUpsertOutcome> UpsertPublicHolidayAsync(int year, DateOnly date, string description, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
