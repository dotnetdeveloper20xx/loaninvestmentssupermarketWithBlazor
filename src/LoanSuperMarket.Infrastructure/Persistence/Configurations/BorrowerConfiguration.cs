using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class BorrowerConfiguration : IEntityTypeConfiguration<Borrower>
{
    public void Configure(EntityTypeBuilder<Borrower> builder)
    {
        builder.ToTable("Borrowers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DateOfBirth)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(BorrowerStatus.PendingVerification)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UserId)
            .HasMaxLength(450);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.UserId);
    }
}