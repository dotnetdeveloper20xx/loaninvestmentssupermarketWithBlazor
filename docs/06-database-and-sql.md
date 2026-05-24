# Database & SQL — The Complete Bible

## Database Architecture

Two EF Core DbContexts, one SQL Server database:

**ApplicationDbContext** — Business data:
- `Lenders` — Funding sources
- `Borrowers` — Loan recipients
- `LoanProducts` — Loan templates
- `LoanApplications` — Loan requests
- `ApplicationDocuments` — Uploaded files
- `RepaymentSchedules` — Amortization plans
- `Installments` — Monthly payments
- `AuditLogs` — Activity history

**AuthIdentityDbContext** — Identity data:
- `AspNetUsers` (ApplicationUser) — User accounts
- `AspNetRoles` (CustomRole) — Roles with descriptions
- `AspNetUserRoles` — User-role assignments
- `RefreshTokens` — JWT refresh tokens
- `UserSessions` — Active sessions
- `RolePermissions` — Granular permissions
- `RecoveryCodes` — 2FA recovery codes

---

## Table: `RepaymentSchedules`

**Business meaning:** The contract between lender and borrower. Created once
when a loan is funded. Contains the agreed financial terms.

**Key columns:**
- `FundedAmount` decimal(18,2) — The principal
- `AnnualInterestRate` decimal(8,4) — Stored to 4 decimal places for precision
- `MonthlyEmi` decimal(18,2) — Fixed monthly payment
- `Performance` int — 1=OnTime, 2=Late, 3=Defaulted

**Relationships:**
- FK to `LoanApplications` (Restrict delete — can't delete an app with a schedule)
- FK to `Lenders` (Restrict delete — can't delete a lender with active loans)
- Has many `Installments` (Cascade delete — if schedule is deleted, installments go too)

**Indexes:**
- `IX_RepaymentSchedules_LoanApplicationId`
- `IX_RepaymentSchedules_LenderId`
- `IX_RepaymentSchedules_Performance`

---

## Table: `Installments`

**Business meaning:** One monthly payment. 36-month loan = 36 rows.

**Key columns:**
- `InstallmentNumber` int — Sequential (1, 2, 3...)
- `DueDate` datetime2 — When payment is expected
- `PrincipalPortion` decimal(18,2) — Goes toward reducing the loan
- `InterestPortion` decimal(18,2) — Lender's profit
- `TotalAmount` decimal(18,2) — What borrower pays
- `Status` int — The state machine value
- `PaidAmount` decimal(18,2) — Cumulative payments received
- `LateFeeAmount` decimal(18,2) — Penalty for late payment
- `ReminderSent` bit — Prevents duplicate notifications
- `LateNoticeSent` bit — Prevents duplicate notices

**Unique constraint:** `(RepaymentScheduleId, InstallmentNumber)` — You can't
have two installment #3s in the same schedule.

**Indexes:**
- `IX_Installments_RepaymentScheduleId`
- `IX_Installments_DueDate` — For the background service to find overdue ones
- `IX_Installments_Status` — For filtering by payment status

---

## Stored Procedure: `sp_GetPlatformSummary`

**Purpose:** Single-query platform KPIs for the admin dashboard.

**Why a stored procedure?** This touches 4 tables with COUNT and SUM
aggregations. EF Core would generate multiple queries or a complex LINQ
expression. The stored procedure runs as one optimized execution plan.

**Returns:**
- ActiveLoans, DefaultedLoans (from RepaymentSchedules)
- TotalFunded, TotalCollected (from RepaymentSchedules + Installments)
- TotalInterestCollected, TotalLateFeesCollected (from Installments)
- ActiveLenders, ActiveBorrowers (from Lenders, Borrowers)
- TotalAvailableCapital (from Lenders)

**Called by:** `DapperPlatformReportService` via Dapper (not EF Core).

---

## Stored Procedure: `sp_GetMonthlyInterestReport`

**Purpose:** Monthly income breakdown for a specific lender.

**Parameters:** @LenderId, @FromDate, @ToDate

**Logic:** Joins Installments with RepaymentSchedules, filters by lender and
date range, groups by year/month, sums interest, principal, and late fees.

**Called by:** `DapperPlatformReportService.GetMonthlyInterestReportAsync()`

---

## EF Core Configuration Patterns

Every entity has a configuration class implementing
`IEntityTypeConfiguration<T>`. Key patterns:

**Enum storage:** `HasConversion<int>()` — Stores enums as integers, not
strings. More efficient for indexing and storage.

**Decimal precision:** `HasPrecision(18, 2)` — 18 total digits, 2 after
decimal. Standard for financial amounts.

**Backing field access:** For RepaymentSchedule's Installments collection:
```csharp
builder.Navigation(x => x.Installments)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```
This tells EF to read/write through the private `_installments` field.

---

## Migration Strategy

Migrations are generated with:
```
dotnet ef migrations add Name --context ApplicationDbContext
```

A `ApplicationDbContextFactory` (design-time factory) exists because the
normal host builder fails during migration generation. It reads the
connection string directly from appsettings.json.

Current migrations:
1. `InitialMigration` — All base tables
2. `AddAuditLogs` — AuditLogs table
3. `AddRepaymentScheduleAndInstallments` — Schedules + Installments + CreditTier on Borrowers
