using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("LoanApplications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BorrowerId)
            .IsRequired();

        builder.Property(x => x.LoanProductId)
            .IsRequired(false);

        builder.OwnsOne(x => x.RequestedAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("RequestedAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(x => x.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.TermMonths)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LoanApplicationStatus.Draft)
            .IsRequired();

        builder.Property(x => x.SubmittedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ReviewedBy)
            .HasMaxLength(450);

        builder.Property(x => x.ReviewReason)
            .HasMaxLength(2000);

        builder.Property(x => x.ReviewedAtUtc);

        builder.Property(x => x.DocumentRequestNote)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.LoanApplication)
            .HasForeignKey(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BorrowerId);
        builder.HasIndex(x => x.LoanProductId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SubmittedAtUtc);
    }
}
