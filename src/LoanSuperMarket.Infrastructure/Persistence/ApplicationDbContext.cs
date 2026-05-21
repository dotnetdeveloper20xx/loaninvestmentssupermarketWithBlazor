using LoanSuperMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace LoanSuperMarket.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();

    public DbSet<Borrower> Borrowers => Set<Borrower>();

    public DbSet<Lender> Lenders => Set<Lender>();

    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}