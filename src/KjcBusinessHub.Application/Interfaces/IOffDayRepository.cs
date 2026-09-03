using KjcBusinessHub.Application.Entities;
namespace KjcBusinessHub.Application.Interfaces;

public enum PublicHolidayUpsertOutcome
{
    Inserted,
    Updated,
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
    /// If an entry with the same date already exists, its public-holiday flag and description are updated
    /// while preserving any existing vacation flag.
    /// Returns whether the public holiday was inserted or updated.
    /// </summary>
    Task<PublicHolidayUpsertOutcome> UpsertPublicHolidayAsync(int year, DateOnly date, string description, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
