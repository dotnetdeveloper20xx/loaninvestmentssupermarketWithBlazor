using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("Installments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RepaymentScheduleId)
            .IsRequired();

        builder.Property(x => x.InstallmentNumber)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.PrincipalPortion)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InterestPortion)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.RemainingBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(InstallmentStatus.Pending)
            .IsRequired();

        builder.Property(x => x.PaidAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.LateFeeAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.ReminderSent)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.LateNoticeSent)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.HasIndex(x => x.RepaymentScheduleId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.RepaymentScheduleId, x.InstallmentNumber }).IsUnique();
    }
}
