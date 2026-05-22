using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.ToTable("ApplicationDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LoanApplicationId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.StorageReference)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.UploadedAtUtc)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(DocumentStatus.Pending)
            .IsRequired();

        builder.Property(x => x.VerifiedBy)
            .HasMaxLength(450);

        builder.Property(x => x.VerifiedAtUtc);

        builder.Property(x => x.RejectionNote)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(150);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(150);

        builder.HasOne(x => x.LoanApplication)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LoanApplicationId);
        builder.HasIndex(x => x.Status);
    }
}
