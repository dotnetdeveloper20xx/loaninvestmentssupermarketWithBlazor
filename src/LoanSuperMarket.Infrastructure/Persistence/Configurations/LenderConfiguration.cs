using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class LenderConfiguration : IEntityTypeConfiguration<Lender>
{
    public void Configure(EntityTypeBuilder<Lender> builder)
    {
        builder.ToTable("Lenders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ContactName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AvailableFunds)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LenderStatus.PendingVerification)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UserId)
            .HasMaxLength(450);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.UserId);
    }
}