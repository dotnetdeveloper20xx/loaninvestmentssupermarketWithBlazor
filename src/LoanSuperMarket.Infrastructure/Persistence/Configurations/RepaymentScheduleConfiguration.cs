using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class RepaymentScheduleConfiguration : IEntityTypeConfiguration<RepaymentSchedule>
{
    public void Configure(EntityTypeBuilder<RepaymentSchedule> builder)
    {
        builder.ToTable("RepaymentSchedules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LoanApplicationId)
            .IsRequired();

        builder.Property(x => x.LenderId)
            .IsRequired();

        builder.Property(x => x.FundedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.AnnualInterestRate)
            .HasPrecision(8, 4)
            .IsRequired();

        builder.Property(x => x.TermMonths)
            .IsRequired();

        builder.Property(x => x.MonthlyEmi)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalInterestPayable)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Performance)
            .HasConversion<int>()
            .HasDefaultValue(LoanPerformance.OnTime)
            .IsRequired();

        builder.Property(x => x.GeneratedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.HasOne(x => x.LoanApplication)
            .WithMany()
            .HasForeignKey(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Lender)
            .WithMany()
            .HasForeignKey(x => x.LenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Installments)
            .WithOne()
            .HasForeignKey(x => x.RepaymentScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Installments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.LoanApplicationId);
        builder.HasIndex(x => x.LenderId);
        builder.HasIndex(x => x.Performance);
    }
}
