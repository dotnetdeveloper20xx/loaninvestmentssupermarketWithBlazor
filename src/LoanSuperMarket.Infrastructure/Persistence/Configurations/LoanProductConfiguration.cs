using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class LoanProductConfiguration : IEntityTypeConfiguration<LoanProduct>
{
    public void Configure(EntityTypeBuilder<LoanProduct> builder)
    {
        builder.ToTable("LoanProducts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.LenderId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LoanProductStatus.Draft)
            .IsRequired();

        builder.OwnsOne(x => x.MinimumAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("MinimumAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(x => x.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.MaximumAmount, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("MaximumAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(x => x.Currency)
                .HasColumnName("MaximumAmountCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.InterestRate, rate =>
        {
            rate.Property(x => x.Percentage)
                .HasColumnName("InterestRate")
                .HasPrecision(5, 2)
                .IsRequired();
        });

        builder.Property(x => x.MinimumTermMonths)
            .IsRequired();

        builder.Property(x => x.MaximumTermMonths)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedAtUtc);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LenderId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}