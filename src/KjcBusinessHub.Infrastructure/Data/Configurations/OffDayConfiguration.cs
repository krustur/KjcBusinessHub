using KjcBusinessHub.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KjcBusinessHub.Infrastructure.Data.Configurations;

public class OffDayConfiguration : IEntityTypeConfiguration<OffDay>
{
    public void Configure(EntityTypeBuilder<OffDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Year)
            .IsRequired();

        builder.Property(d => d.Date)
            .IsRequired();

        builder.Property(d => d.IsPublicHoliday)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.PublicHolidayDescription)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(d => d.IsVacation)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.HasIndex(d => new { d.Year, d.Date })
            .IsUnique();
    }
}
