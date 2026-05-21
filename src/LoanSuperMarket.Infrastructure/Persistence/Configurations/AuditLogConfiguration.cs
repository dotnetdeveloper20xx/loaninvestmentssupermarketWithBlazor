using LoanSuperMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityId);

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.PerformedBy)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}