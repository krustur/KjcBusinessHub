using KjcBusinessHub.Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KjcBusinessHub.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Ignore(t => t.LinkedSourceDocumentCount);
        builder.Ignore(t => t.IsLinked);
        builder.Ignore(t => t.HasMultipleLinkedSourceDocuments);
        builder.Ignore(t => t.IsHandled);
        builder.Ignore(t => t.TransactionTypeDisplay);

        builder.Property(t => t.IsHandledWithoutDocument)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasMany(t => t.SourceDocuments)
            .WithMany(d => d.Transactions)
            .UsingEntity("TransactionSourceDocuments",
                l => l.HasOne(typeof(SourceDocument)).WithMany().HasForeignKey("SourceDocumentId"),
                r => r.HasOne(typeof(Transaction)).WithMany().HasForeignKey("TransactionId"),
                j =>
                {
                    j.Property<DateTimeOffset>("CreatedAt").IsRequired();
                    j.HasKey("TransactionId", "SourceDocumentId");
                });
    }
}
