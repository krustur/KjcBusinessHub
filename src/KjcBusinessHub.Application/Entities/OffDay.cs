using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

public class OffDay
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public DateOnly Date { get; set; }
    public bool IsPublicHoliday { get; set; }
    public string PublicHolidayDescription { get; set; } = string.Empty;
    public AbsenceType AbsenceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public void Validate()
    {
        if (!Enum.IsDefined(AbsenceType))
            throw new ArgumentException($"OffDay {Date} has an unsupported absence type.", nameof(OffDay));

        if (!IsPublicHoliday && AbsenceType == AbsenceType.None)
            throw new ArgumentException($"OffDay {Date} must be either a public holiday or an absence day.", nameof(OffDay));

        if (IsPublicHoliday && string.IsNullOrWhiteSpace(PublicHolidayDescription))
            throw new ArgumentException($"OffDay {Date} must have a public-holiday description.", nameof(OffDay));

        if (!IsPublicHoliday && !string.IsNullOrWhiteSpace(PublicHolidayDescription))
            throw new ArgumentException($"OffDay {Date} cannot have a public-holiday description without being a public holiday.", nameof(OffDay));
    }
}
