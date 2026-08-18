using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KjcBusinessHub.Infrastructure.Data.Configurations;

public class SourceDocumentConfiguration : IEntityTypeConfiguration<SourceDocument>
{
    public void Configure(EntityTypeBuilder<SourceDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileSubPath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.FileHash)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Ccy)
            .HasConversion<string>()
            .HasMaxLength(3);

        builder.Property(d => d.CcyAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Status)
            .IsRequired();

        builder.Property(d => d.FileCreatedDate)
            .IsRequired();

        builder.Property(d => d.FileModifiedDate)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Ignore(d => d.CurrencyDisplay);
        builder.Ignore(d => d.LinkedTransactionCount);
        builder.Ignore(d => d.IsLinked);
        builder.Ignore(d => d.HasMultipleLinkedTransactions);
        builder.Ignore(d => d.IsAnnual);
        builder.Ignore(d => d.IsExpiredAnnual);

        builder.Property(d => d.IsFutureTransaction)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.AnnualType)
            .IsRequired()
            .HasDefaultValue(SourceDocumentAnnualType.NotAnnual);
    }
}
