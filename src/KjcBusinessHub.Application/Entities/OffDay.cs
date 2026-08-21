using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.Application.Entities;

public class OffDay
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public DateOnly Date { get; set; }
    public OffDayType OffDayType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
