namespace KjcBusinessHub.Application.Entities;

public class OffDay
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public DateOnly Date { get; set; }
    public bool IsPublicHoliday { get; set; }
    public string PublicHolidayDescription { get; set; } = string.Empty;
    public bool IsVacation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
