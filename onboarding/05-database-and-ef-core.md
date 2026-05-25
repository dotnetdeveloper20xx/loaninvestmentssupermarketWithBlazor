# 05 — Database and Entity Framework Core

## Table of Contents

1. [Two DbContexts: Why and How](#two-dbcontexts-why-and-how)
2. [ApplicationDbContext (Business Data)](#applicationdbcontext-business-data)
3. [AuthIdentityDbContext (Identity Data)](#authidentitydbcontext-identity-data)
4. [Entity Configurations (IEntityTypeConfiguration)](#entity-configurations-ientitytypeconfiguration)
5. [Value Object Mapping (OwnsOne)](#value-object-mapping-ownsone)
6. [Indexes, Precision, and Default Values](#indexes-precision-and-default-values)
7. [Design-Time Factory for Migrations](#design-time-factory-for-migrations)
8. [Migration Commands](#migration-commands)
9. [Development Data Seeder](#development-data-seeder)
10. [Full Entity Configuration Examples](#full-entity-configuration-examples)
11. [Adding a New Entity Step-by-Step](#adding-a-new-entity-step-by-step)

---

## Two DbContexts: Why and How

LoanSuperMarket uses **two separate DbContexts**, each targeting the same SQL Server database
but managing different concerns:

| Context | Purpose | Base Class | Location |
|---------|---------|-----------|----------|
| `ApplicationDbContext` | Business entities (loans, products, borrowers) | `DbContext` | `Infrastructure/Persistence/` |
| `AuthIdentityDbContext` | Authentication & identity (users, roles, tokens) | `IdentityDbContext<>` | `Infrastructure/Identity/` |

### Why Two Contexts?

1. **Separation of concerns** — Identity is a cross-cutting infrastructure concern, not a
   business domain concept. Keeping it separate means business code never accidentally
   queries user tables directly.

2. **Different base classes** — `AuthIdentityDbContext` inherits from
   `IdentityDbContext<ApplicationUser, CustomRole, string>` which provides all the ASP.NET
   Identity tables (AspNetUsers, AspNetRoles, etc.). `ApplicationDbContext` inherits from
   plain `DbContext`.

3. **Independent migrations** — Each context has its own migration history. You can evolve
   business schema without touching identity, and vice versa.

4. **Clean Architecture compliance** — The Application layer depends on `ApplicationDbContext`
   for business operations. It never needs to know about Identity internals.

5. **Testability** — You can mock or use in-memory versions of each context independently.

### Connection String

Both contexts share the same connection string (same database):

```json
// File: src/LoanSuperMarket.Api/appsettings.json

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-VVJN96B;Database=LoanSuperMarketDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

---

## ApplicationDbContext (Business Data)

This is the primary context for all business operations.

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/ApplicationDbContext.cs

using LoanSuperMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ─── Business Entity DbSets ─────────────────────────────────────────

    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();
    // Loan products offered by lenders (e.g., "Personal Growth Loan", "Business Expansion Loan")

    public DbSet<Borrower> Borrowers => Set<Borrower>();
    // People who apply for loans

    public DbSet<Lender> Lenders => Set<Lender>();
    // Companies/individuals who provide loan capital

    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    // Applications submitted by borrowers for specific loan products

    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    // Documents uploaded as part of loan applications (ID, payslips, etc.)

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    // Immutable audit trail of all significant actions

    public DbSet<RepaymentSchedule> RepaymentSchedules => Set<RepaymentSchedule>();
    // Amortization schedules for funded loans

    public DbSet<Installment> Installments => Set<Installment>();
    // Individual monthly payments within a repayment schedule

    // ─── Configuration ──────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-discover all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

### DbSet Properties Explained

| DbSet | Table Name | Description |
|-------|-----------|-------------|
| `LoanProducts` | LoanProducts | Catalog of available loan products with rates, terms, limits |
| `Borrowers` | Borrowers | Registered borrowers with KYC status |
| `Lenders` | Lenders | Registered lenders with available capital |
| `LoanApplications` | LoanApplications | Applications with full lifecycle (Draft → Funded) |
| `ApplicationDocuments` | ApplicationDocuments | Uploaded files linked to applications |
| `AuditLogs` | AuditLogs | Who did what, when, to which entity |
| `RepaymentSchedules` | RepaymentSchedules | Amortization plans for funded loans |
| `Installments` | Installments | Individual EMI payments with status tracking |

### The `Set<T>()` Pattern

Notice we use `=> Set<LoanProduct>()` instead of `{ get; set; }`. This is the modern
expression-bodied property pattern that avoids nullable warnings and ensures the DbSet
is always available.

```csharp
// ✅ Modern pattern (no null warnings)
public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();

// ❌ Old pattern (requires null-forgiving operator)
public DbSet<LoanProduct> LoanProducts { get; set; } = null!;
```

---

## AuthIdentityDbContext (Identity Data)

Manages ASP.NET Identity tables plus custom extensions.

```csharp
// File: src/LoanSuperMarket.Infrastructure/Identity/AuthIdentityDbContext.cs

using LoanSuperMarket.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Identity;

public sealed class AuthIdentityDbContext : IdentityDbContext<ApplicationUser, CustomRole, string>
{
    public AuthIdentityDbContext(DbContextOptions<AuthIdentityDbContext> options)
        : base(options)
    {
    }

    // ─── Custom Identity Extensions ─────────────────────────────────────

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // JWT refresh tokens for session management

    public DbSet<UserSession> UserSessions => Set<UserSession>();
    // Active user sessions with device/browser info

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    // Fine-grained permissions assigned to roles

    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
    // 2FA recovery codes for account recovery

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANT: Call base first — configures all Identity tables
        base.OnModelCreating(modelBuilder);

        // Then apply our custom configurations
        ConfigureRefreshToken(modelBuilder);
        ConfigureUserSession(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureRecoveryCode(modelBuilder);
        ConfigureApplicationUser(modelBuilder);
        ConfigureCustomRole(modelBuilder);
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(512);

            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.ReplacedByToken).HasMaxLength(512);
            entity.Property(e => e.RevokedReason).HasMaxLength(256);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.RefreshTokenId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.DeviceType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Browser).HasMaxLength(256);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RoleId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.RoleId);
            entity.Property(e => e.GrantedBy).HasMaxLength(450);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRecoveryCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecoveryCode>(entity =>
        {
            entity.ToTable("RecoveryCodes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(128);
        });
    }

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AccountStatusReason).HasMaxLength(500);
            entity.Property(e => e.AccountStatusChangedBy).HasMaxLength(450);
            entity.Property(e => e.BlockedActivity).HasMaxLength(50);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CapitalLimit).HasPrecision(18, 2);
        });
    }

    private static void ConfigureCustomRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
        });
    }
}
```

### Key Differences from ApplicationDbContext

| Aspect | ApplicationDbContext | AuthIdentityDbContext |
|--------|---------------------|---------------------|
| Base class | `DbContext` | `IdentityDbContext<ApplicationUser, CustomRole, string>` |
| Configuration style | Separate `IEntityTypeConfiguration<T>` files | Inline in `OnModelCreating` |
| Auto-discovery | `ApplyConfigurationsFromAssembly` | Manual method calls |
| Tables provided by base | None | AspNetUsers, AspNetRoles, AspNetUserRoles, etc. |
| Primary key type | `Guid` | `string` (Identity default) |

---

## Entity Configurations (IEntityTypeConfiguration)

For the `ApplicationDbContext`, each entity has a dedicated configuration class that implements
`IEntityTypeConfiguration<T>`. This keeps the DbContext clean and each entity's mapping in its
own file.

### File Location

```
src/LoanSuperMarket.Infrastructure/
└── Persistence/
    └── Configurations/
        ├── ApplicationDocumentConfiguration.cs
        ├── AuditLogConfiguration.cs
        ├── BorrowerConfiguration.cs
        ├── InstallmentConfiguration.cs
        ├── LenderConfiguration.cs
        ├── LoanApplicationConfiguration.cs
        ├── LoanProductConfiguration.cs
        └── RepaymentScheduleConfiguration.cs
```

### How Auto-Discovery Works

In `ApplicationDbContext.OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Scans the Infrastructure assembly for all classes implementing
    // IEntityTypeConfiguration<T> and applies them
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    base.OnModelCreating(modelBuilder);
}
```

This means: **just create a new configuration class** — it's automatically picked up.
No registration needed.

### The Pattern

Every configuration class follows this structure:

```csharp
using LoanSuperMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        // 1. Table name
        builder.ToTable("{TableName}");

        // 2. Primary key
        builder.HasKey(x => x.Id);

        // 3. Property configurations (lengths, precision, required)
        // 4. Value object mappings (OwnsOne)
        // 5. Enum conversions
        // 6. Relationships (HasOne, HasMany)
        // 7. Indexes
    }
}
```

---

## Value Object Mapping (OwnsOne)

Domain-Driven Design uses value objects to represent concepts like Money and InterestRate.
EF Core maps these using `OwnsOne`, which flattens the value object's properties into the
parent table's columns.

### The Money Value Object

In the domain layer, `Money` encapsulates an amount and currency:

```csharp
// Conceptual: Domain/ValueObjects/Money.cs
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }  // e.g., "GBP"

    public static Money Create(decimal amount, string currency = "GBP") => ...;
}
```

### How It's Mapped in LoanProductConfiguration

```csharp
// Maps Money value object to columns in the LoanProducts table
builder.OwnsOne(x => x.MinimumAmount, money =>
{
    money.Property(x => x.Amount)
        .HasColumnName("MinimumAmount")    // Column name in DB
        .HasPrecision(18, 2)              // decimal(18,2) for currency
        .IsRequired();

    money.Property(x => x.Currency)
        .HasColumnName("Currency")         // Shared currency column
        .HasMaxLength(3)                   // "GBP", "USD", "EUR"
        .IsRequired();
});

builder.OwnsOne(x => x.MaximumAmount, money =>
{
    money.Property(x => x.Amount)
        .HasColumnName("MaximumAmount")
        .HasPrecision(18, 2)
        .IsRequired();

    money.Property(x => x.Currency)
        .HasColumnName("MaximumAmountCurrency")  // Different column name to avoid conflict
        .HasMaxLength(3)
        .IsRequired();
});
```

### The InterestRate Value Object

```csharp
builder.OwnsOne(x => x.InterestRate, rate =>
{
    rate.Property(x => x.Percentage)
        .HasColumnName("InterestRate")
        .HasPrecision(5, 2)    // Up to 999.99%
        .IsRequired();
});
```

### Resulting Database Columns

The `LoanProducts` table ends up with these columns (from value objects):

| Column Name | Type | Source |
|-------------|------|--------|
| MinimumAmount | decimal(18,2) | `MinimumAmount.Amount` |
| Currency | nvarchar(3) | `MinimumAmount.Currency` |
| MaximumAmount | decimal(18,2) | `MaximumAmount.Amount` |
| MaximumAmountCurrency | nvarchar(3) | `MaximumAmount.Currency` |
| InterestRate | decimal(5,2) | `InterestRate.Percentage` |

### In LoanApplicationConfiguration

```csharp
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
```

### Why OwnsOne?

- **No separate table** — value object properties are stored in the parent table
- **No foreign key** — it's not a relationship, it's composition
- **Encapsulation preserved** — the domain model uses `Money` objects, not raw decimals
- **Type safety** — you can't accidentally compare a Money amount to an InterestRate percentage

---

## Indexes, Precision, and Default Values

### Indexes

Indexes improve query performance for frequently filtered/sorted columns.

```csharp
// Single-column indexes
builder.HasIndex(x => x.Status);           // Filter by status
builder.HasIndex(x => x.LenderId);         // Filter by lender
builder.HasIndex(x => x.CreatedAtUtc);     // Sort by creation date
builder.HasIndex(x => x.BorrowerId);       // Filter by borrower

// Unique indexes (enforce uniqueness at DB level)
builder.HasIndex(x => x.Email).IsUnique(); // No duplicate emails

// Composite indexes (multi-column)
builder.HasIndex(x => new { x.RepaymentScheduleId, x.InstallmentNumber }).IsUnique();
// Each schedule has unique installment numbers (1, 2, 3...)
```

### Precision Settings

For decimal columns, always specify precision to avoid SQL Server's default (18,0):

```csharp
// Currency amounts: 18 digits total, 2 decimal places
// Supports up to 9,999,999,999,999,999.99
builder.Property(x => x.FundedAmount)
    .HasPrecision(18, 2);

// Interest rates: 8 digits total, 4 decimal places
// Supports up to 9999.9999%
builder.Property(x => x.AnnualInterestRate)
    .HasPrecision(8, 4);

// Percentage rates: 5 digits total, 2 decimal places
// Supports up to 999.99%
rate.Property(x => x.Percentage)
    .HasPrecision(5, 2);
```

### Default Values

Set database-level defaults for columns:

```csharp
// Enum defaults (stored as int)
builder.Property(x => x.Status)
    .HasConversion<int>()
    .HasDefaultValue(LoanApplicationStatus.Draft)
    .IsRequired();

// Boolean defaults
builder.Property(x => x.ReminderSent)
    .HasDefaultValue(false)
    .IsRequired();

// Decimal defaults
builder.Property(x => x.PaidAmount)
    .HasPrecision(18, 2)
    .HasDefaultValue(0m)
    .IsRequired();
```

### Enum Conversion

Enums are stored as integers in the database:

```csharp
builder.Property(x => x.Status)
    .HasConversion<int>()              // Store as int, not string
    .HasDefaultValue(LoanProductStatus.Draft)  // Default to Draft
    .IsRequired();
```

This means `LoanProductStatus.Draft = 0`, `Published = 1`, etc. in the database.

### String Length Constraints

Always set `HasMaxLength` to match your validation rules:

```csharp
builder.Property(x => x.Title)
    .HasMaxLength(150)     // nvarchar(150) in SQL Server
    .IsRequired();         // NOT NULL

builder.Property(x => x.Description)
    .HasMaxLength(2000)    // nvarchar(2000)
    .IsRequired();

builder.Property(x => x.CreatedBy)
    .HasMaxLength(150);    // nullable (no IsRequired)
```

---

## Design-Time Factory for Migrations

EF Core migrations need to create a `DbContext` instance at design time (when you run
`dotnet ef migrations add`). Since the full application host isn't available during migrations,
we provide a factory.

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/ApplicationDbContextFactory.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LoanSuperMarket.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Used when the application host cannot be built (e.g. missing DI registrations at design time).
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Build configuration by reading appsettings.json from the API project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "LoanSuperMarket.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // 2. Get the connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 3. Configure DbContext options with SQL Server
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // 4. Return a new context instance
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
```

### Why This Exists

When you run migration commands, EF Core tools need to:
1. Create a `DbContext` instance
2. Compare the current model to the last migration
3. Generate a new migration file

Without this factory, you'd get:
```
Unable to create an object of type 'ApplicationDbContext'.
```

### How It Finds appsettings.json

The factory navigates from the Infrastructure project to the API project:
```
Current directory: src/LoanSuperMarket.Infrastructure/
Navigate to:      src/LoanSuperMarket.Api/appsettings.json
```

This is why migration commands must be run from the `src/LoanSuperMarket.Infrastructure` directory.

---

## Migration Commands

All migration commands are run from the **Infrastructure project directory**.

### Prerequisites

```bash
# Install EF Core tools globally (one-time)
dotnet tool install --global dotnet-ef
```

### Add a New Migration

```bash
# Navigate to the Infrastructure project
cd src/LoanSuperMarket.Infrastructure

# Add migration for ApplicationDbContext
dotnet ef migrations add AddNewFeature --context ApplicationDbContext --output-dir Persistence/Migrations

# Add migration for AuthIdentityDbContext
dotnet ef migrations add AddNewIdentityFeature --context AuthIdentityDbContext --output-dir Identity/Migrations
```

### Apply Migrations (Update Database)

```bash
# Apply all pending migrations for ApplicationDbContext
dotnet ef database update --context ApplicationDbContext

# Apply all pending migrations for AuthIdentityDbContext
dotnet ef database update --context AuthIdentityDbContext
```

### Remove Last Migration (if not yet applied)

```bash
dotnet ef migrations remove --context ApplicationDbContext
```

### Drop Database (development only!)

```bash
dotnet ef database drop --context ApplicationDbContext --force
```

### Generate SQL Script (for production deployments)

```bash
# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --context ApplicationDbContext --idempotent --output migration.sql
```

### Common Workflow

1. Make changes to domain entities or configurations
2. Add a migration: `dotnet ef migrations add DescriptiveName --context ApplicationDbContext --output-dir Persistence/Migrations`
3. Review the generated migration file
4. Apply: `dotnet ef database update --context ApplicationDbContext`
5. Test the application

### Migration Naming Conventions

Use descriptive names that explain what changed:
- `AddRepaymentScheduleTable`
- `AddIndexOnLoanApplicationStatus`
- `AddCreditTierToBorrower`
- `IncreaseDescriptionMaxLength`

---

## Development Data Seeder

The `DevelopmentDataSeeder` creates comprehensive demo data for local development. It's
**idempotent** — running it multiple times won't create duplicate data.

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/DevelopmentDataSeeder.cs

using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Persistence;

/// <summary>
/// Seeds comprehensive development/demo data for the lending lifecycle.
/// Creates users, lenders, borrowers, products, applications in various states,
/// funded loans with repayment schedules, and audit logs.
/// Idempotent — skips if data already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string DefaultPassword = "Demo@12345!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // ⚡ IDEMPOTENCY CHECK — if schedules exist, data was already seeded
        if (await context.RepaymentSchedules.AnyAsync())
        {
            logger.LogDebug("Development data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding comprehensive development data...");

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: Create Identity Users
        // ═══════════════════════════════════════════════════════════════
        await SeedUsersAsync(userManager, logger);

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: Create Lenders (business entities)
        // ═══════════════════════════════════════════════════════════════
        var lenders = new[]
        {
            Lender.Create("Apex Capital Partners", "James Richardson",
                "lender1@demo.com", "020 7946 0001", 1_200_000m),
            Lender.Create("Sterling Finance Group", "Sarah Mitchell",
                "lender2@demo.com", "020 7946 0002", 850_000m),
            Lender.Create("Meridian Investments", "Robert Clarke",
                "lender3@demo.com", "020 7946 0003", 600_000m),
            Lender.Create("Horizon Capital Ltd", "Victoria Adams",
                "lender4@demo.com", "020 7946 0004", 950_000m),
            Lender.Create("Pinnacle Lending Co", "Thomas Wright",
                "lender5@demo.com", "020 7946 0005", 400_000m),
        };

        foreach (var lender in lenders) lender.Verify();
        await context.Lenders.AddRangeAsync(lenders);

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: Create Borrowers
        // ═══════════════════════════════════════════════════════════════
        var borrowers = new[]
        {
            Borrower.Create("Michael", "Thompson", "borrower1@demo.com",
                "07700 900001", new DateTime(1985, 3, 15)),
            Borrower.Create("Emma", "Williams", "borrower2@demo.com",
                "07700 900002", new DateTime(1990, 7, 22)),
            Borrower.Create("David", "Chen", "borrower3@demo.com",
                "07700 900003", new DateTime(1988, 11, 8)),
            Borrower.Create("Sophie", "Taylor", "borrower4@demo.com",
                "07700 900004", new DateTime(1992, 5, 30)),
            Borrower.Create("James", "Patel", "borrower5@demo.com",
                "07700 900005", new DateTime(1987, 9, 12)),
        };

        foreach (var borrower in borrowers) borrower.Verify();
        await context.Borrowers.AddRangeAsync(borrowers);

        // ⚡ SaveChanges between layers — entities need IDs before linking
        await context.SaveChangesAsync();

        // Link borrowers and lenders to their user accounts
        // (sets the UserId shadow property)
        // ... linking code ...

        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: Create Loan Products (4 per lender = 20 total)
        // ═══════════════════════════════════════════════════════════════
        var products = CreateLoanProducts(lenders);
        await context.LoanProducts.AddRangeAsync(products);
        await context.SaveChangesAsync();  // Products need IDs for applications

        // ═══════════════════════════════════════════════════════════════
        // STEP 5: Create Loan Applications in various states
        // ═══════════════════════════════════════════════════════════════
        var publishedProducts = products.Where(p => p.Status == LoanProductStatus.Published).ToList();
        var applications = CreateLoanApplications(borrowers, publishedProducts);
        await context.LoanApplications.AddRangeAsync(applications);
        await context.SaveChangesAsync();  // Applications need IDs for schedules

        // ═══════════════════════════════════════════════════════════════
        // STEP 6: Generate Repayment Schedules for funded loans
        // ═══════════════════════════════════════════════════════════════
        var amortizationService = scope.ServiceProvider
            .GetRequiredService<IAmortizationService>();
        var fundedApps = applications
            .Where(a => a.Status == LoanApplicationStatus.Funded).ToList();

        foreach (var fundedApp in fundedApps)
        {
            var product = publishedProducts.First(p => p.Id == fundedApp.LoanProductId);
            var lender = lenders.First(l => l.Id == product.LenderId);
            var effectiveRate = product.InterestRate.Percentage + 2m;

            var monthsAgo = Random.Shared.Next(2, 8);
            var schedule = amortizationService.GenerateSchedule(
                fundedApp.Id, lender.Id, fundedApp.RequestedAmount.Amount,
                effectiveRate, fundedApp.TermMonths,
                DateTime.UtcNow.AddMonths(-monthsAgo));

            // Simulate past payments
            var installments = schedule.Installments
                .OrderBy(i => i.InstallmentNumber).ToList();
            var paymentsMade = Math.Min(monthsAgo - 1, installments.Count);
            for (var i = 0; i < paymentsMade; i++)
            {
                installments[i].RecordFullPayment(
                    installments[i].DueDate.AddDays(Random.Shared.Next(0, 3)));
            }

            lender.DeductFunds(fundedApp.RequestedAmount.Amount);
            await context.RepaymentSchedules.AddAsync(schedule);
        }

        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════
        // STEP 7: Create Audit Logs
        // ═══════════════════════════════════════════════════════════════
        var auditLogs = new[]
        {
            AuditLog.Create("LoanApplication", applications[0].Id,
                "Submitted", "Loan application submitted for review.",
                "borrower1@demo.com"),
            AuditLog.Create("LoanApplication", applications[1].Id,
                "Approved", "Application approved after credit check.",
                "crm1@demo.com"),
            // ... more audit logs ...
        };

        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();

        logger.LogInformation("Development data seeded successfully.");
    }
}
```

### Key Seeder Design Patterns

1. **Idempotency** — Checks `RepaymentSchedules.AnyAsync()` before seeding. If data exists,
   it exits immediately. This makes it safe to call on every startup.

2. **Dependency ordering** — Entities are created in order of their dependencies:
   - Users first (no dependencies)
   - Lenders and Borrowers (depend on Users for linking)
   - Products (depend on Lenders)
   - Applications (depend on Borrowers and Products)
   - Schedules (depend on Applications and Lenders)
   - Audit logs (depend on everything)

3. **SaveChanges between layers** — Each layer is saved before the next begins, ensuring
   generated IDs are available for foreign keys.

4. **Realistic data** — Applications exist in every status (Draft, Submitted, UnderReview,
   Approved, Rejected, Funded, DocumentsRequested) for testing all UI states.

5. **Domain method usage** — The seeder uses domain methods (`Lender.Create()`, `app.Submit()`,
   `app.Approve()`) rather than setting properties directly. This ensures all domain invariants
   are respected.

---

## Full Entity Configuration Examples

### LoanProductConfiguration (Value Objects + Enums + Indexes)

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/Configurations/LoanProductConfiguration.cs

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
        // ─── Table & Key ────────────────────────────────────────────────
        builder.ToTable("LoanProducts");
        builder.HasKey(x => x.Id);

        // ─── Simple Properties ──────────────────────────────────────────
        builder.Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.LenderId)
            .IsRequired();

        // ─── Enum Conversion ────────────────────────────────────────────
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LoanProductStatus.Draft)
            .IsRequired();

        // ─── Value Object: MinimumAmount (Money) ────────────────────────
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

        // ─── Value Object: MaximumAmount (Money) ────────────────────────
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

        // ─── Value Object: InterestRate ─────────────────────────────────
        builder.OwnsOne(x => x.InterestRate, rate =>
        {
            rate.Property(x => x.Percentage)
                .HasColumnName("InterestRate")
                .HasPrecision(5, 2)
                .IsRequired();
        });

        // ─── Term Properties ────────────────────────────────────────────
        builder.Property(x => x.MinimumTermMonths).IsRequired();
        builder.Property(x => x.MaximumTermMonths).IsRequired();

        // ─── Audit Properties ───────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(150);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy).HasMaxLength(150);

        // ─── Indexes ────────────────────────────────────────────────────
        builder.HasIndex(x => x.Status);        // Filter published products
        builder.HasIndex(x => x.LenderId);      // Products by lender
        builder.HasIndex(x => x.CreatedAtUtc);  // Sort by newest
    }
}
```

### LoanApplicationConfiguration (Relationships + OwnsOne)

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/Configurations/LoanApplicationConfiguration.cs

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

        // ─── Foreign Keys ───────────────────────────────────────────────
        builder.Property(x => x.BorrowerId).IsRequired();
        builder.Property(x => x.LoanProductId).IsRequired(false);
        // LoanProductId is nullable — draft applications may not have a product yet

        // ─── Value Object: RequestedAmount (Money) ──────────────────────
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

        // ─── Simple Properties ──────────────────────────────────────────
        builder.Property(x => x.TermMonths).IsRequired();

        builder.Property(x => x.Purpose)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(LoanApplicationStatus.Draft)
            .IsRequired();

        builder.Property(x => x.SubmittedAtUtc).IsRequired(false);
        builder.Property(x => x.ReviewedBy).HasMaxLength(450);
        builder.Property(x => x.ReviewReason).HasMaxLength(2000);
        builder.Property(x => x.ReviewedAtUtc);
        builder.Property(x => x.DocumentRequestNote).HasMaxLength(2000);

        // ─── Audit Properties ───────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(150);
        builder.Property(x => x.UpdatedBy).HasMaxLength(150);

        // ─── Relationships ──────────────────────────────────────────────
        builder.HasMany(x => x.Documents)
            .WithOne(x => x.LoanApplication)
            .HasForeignKey(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        // When an application is deleted, all its documents are deleted too

        // ─── Indexes ────────────────────────────────────────────────────
        builder.HasIndex(x => x.BorrowerId);      // My applications
        builder.HasIndex(x => x.LoanProductId);   // Applications per product
        builder.HasIndex(x => x.Status);          // Filter by status
        builder.HasIndex(x => x.SubmittedAtUtc);  // Sort by submission date
    }
}
```

### RepaymentScheduleConfiguration (Complex Relationships)

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/Configurations/RepaymentScheduleConfiguration.cs

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

        // ─── Properties ─────────────────────────────────────────────────
        builder.Property(x => x.LoanApplicationId).IsRequired();
        builder.Property(x => x.LenderId).IsRequired();

        builder.Property(x => x.FundedAmount)
            .HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.AnnualInterestRate)
            .HasPrecision(8, 4).IsRequired();

        builder.Property(x => x.TermMonths).IsRequired();

        builder.Property(x => x.MonthlyEmi)
            .HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.TotalInterestPayable)
            .HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.Performance)
            .HasConversion<int>()
            .HasDefaultValue(LoanPerformance.OnTime)
            .IsRequired();

        builder.Property(x => x.GeneratedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(150);
        builder.Property(x => x.UpdatedBy).HasMaxLength(150);

        // ─── Relationships ──────────────────────────────────────────────

        // One schedule belongs to one loan application
        builder.HasOne(x => x.LoanApplication)
            .WithMany()
            .HasForeignKey(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
        // Restrict: can't delete an application that has a schedule

        // One schedule belongs to one lender
        builder.HasOne(x => x.Lender)
            .WithMany()
            .HasForeignKey(x => x.LenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // One schedule has many installments
        builder.HasMany(x => x.Installments)
            .WithOne()
            .HasForeignKey(x => x.RepaymentScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
        // Cascade: deleting a schedule deletes all its installments

        // Use field access for the Installments collection
        // (the domain entity uses a private backing field)
        builder.Navigation(x => x.Installments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // ─── Indexes ────────────────────────────────────────────────────
        builder.HasIndex(x => x.LoanApplicationId);
        builder.HasIndex(x => x.LenderId);
        builder.HasIndex(x => x.Performance);
    }
}
```

### InstallmentConfiguration (Composite Index + Defaults)

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/Configurations/InstallmentConfiguration.cs

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

        builder.Property(x => x.RepaymentScheduleId).IsRequired();
        builder.Property(x => x.InstallmentNumber).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();

        // Financial amounts with precision
        builder.Property(x => x.PrincipalPortion).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.InterestPortion).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RemainingBalance).HasPrecision(18, 2).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(InstallmentStatus.Pending)
            .IsRequired();

        // Amounts with defaults (start at zero)
        builder.Property(x => x.PaidAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.LateFeeAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.Notes).HasMaxLength(500);

        // Boolean flags with defaults
        builder.Property(x => x.ReminderSent).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.LateNoticeSent).HasDefaultValue(false).IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(150);
        builder.Property(x => x.UpdatedBy).HasMaxLength(150);

        // ─── Indexes ────────────────────────────────────────────────────
        builder.HasIndex(x => x.RepaymentScheduleId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);

        // Composite unique index: each schedule has unique installment numbers
        builder.HasIndex(x => new { x.RepaymentScheduleId, x.InstallmentNumber })
            .IsUnique();
    }
}
```

---

## Adding a New Entity Step-by-Step

Let's walk through adding a hypothetical `PaymentTransaction` entity.

### Step 1: Create the Domain Entity

```csharp
// File: src/LoanSuperMarket.Domain/Entities/PaymentTransaction.cs

namespace LoanSuperMarket.Domain.Entities;

public sealed class PaymentTransaction
{
    public Guid Id { get; private set; }
    public Guid InstallmentId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? TransactionReference { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PaymentTransaction() { } // EF Core constructor

    public static PaymentTransaction Create(
        Guid installmentId, decimal amount, string paymentMethod, string? reference)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            InstallmentId = installmentId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            TransactionReference = reference,
            ProcessedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
```

### Step 2: Add DbSet to ApplicationDbContext

```csharp
// In ApplicationDbContext.cs, add:
public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
```

### Step 3: Create the Entity Configuration

```csharp
// File: src/LoanSuperMarket.Infrastructure/Persistence/Configurations/PaymentTransactionConfiguration.cs

using LoanSuperMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanSuperMarket.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InstallmentId).IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .HasMaxLength(200);

        builder.Property(x => x.ProcessedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Relationship
        builder.HasOne<Installment>()
            .WithMany()
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.InstallmentId);
        builder.HasIndex(x => x.ProcessedAtUtc);
    }
}
```

### Step 4: Add Migration

```bash
cd src/LoanSuperMarket.Infrastructure
dotnet ef migrations add AddPaymentTransactionTable --context ApplicationDbContext --output-dir Persistence/Migrations
```

### Step 5: Apply Migration

```bash
dotnet ef database update --context ApplicationDbContext
```

### Step 6: Verify

Check that the table was created in SQL Server Management Studio or Azure Data Studio.

### Checklist for New Entities

- [ ] Domain entity created with private constructor for EF Core
- [ ] DbSet added to `ApplicationDbContext`
- [ ] Configuration class created implementing `IEntityTypeConfiguration<T>`
- [ ] All decimal properties have `HasPrecision`
- [ ] All string properties have `HasMaxLength`
- [ ] Enum properties have `HasConversion<int>()`
- [ ] Relationships configured with appropriate `OnDelete` behaviour
- [ ] Indexes added for frequently queried columns
- [ ] Migration generated and reviewed
- [ ] Migration applied to development database

---

## Summary

| Concept | Location |
|---------|----------|
| Business DbContext | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| Identity DbContext | `Infrastructure/Identity/AuthIdentityDbContext.cs` |
| Entity configurations | `Infrastructure/Persistence/Configurations/` |
| Design-time factory | `Infrastructure/Persistence/ApplicationDbContextFactory.cs` |
| Data seeder | `Infrastructure/Persistence/DevelopmentDataSeeder.cs` |
| Connection string | `Api/appsettings.json` → `ConnectionStrings:DefaultConnection` |
| Migration output | `Infrastructure/Persistence/Migrations/` |

---

*Previous: [04 — FluentValidation](./04-fluent-validation.md)*
*Next: [06 — Dapper and Stored Procedures](./06-dapper-and-stored-procedures.md)*
