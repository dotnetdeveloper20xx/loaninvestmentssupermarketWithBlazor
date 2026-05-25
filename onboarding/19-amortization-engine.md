# 19 — Amortization Engine

## Feature Requirements

The amortization engine generates repayment schedules when a loan is funded. Key requirements:

1. **EMI Calculation**: Uses the standard EMI formula to calculate equal monthly installments
2. **Schedule Generation**: Creates a `RepaymentSchedule` with individual `Installment` entities
3. **Interest/Principal Split**: Each installment shows how much goes to interest vs principal
4. **Rounding Correction**: Final installment absorbs any rounding differences
5. **Performance Tracking**: Schedule tracks loan performance (OnTime, Late, Defaulted)
6. **Restructuring Support**: Distressed loans can be restructured with new terms

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| EMI Formula | Standard amortization calculation |
| Domain Service | `IAmortizationService` / `AmortizationService` |
| Entity Relationships | `RepaymentSchedule` → `Installment` (1:many) |
| Internal Constructor | `Installment` can only be created by the amortization service |

---

## The EMI Formula

```
EMI = P × r × (1+r)^n / ((1+r)^n - 1)
```

Where:
- **P** = Principal (loan amount)
- **r** = Monthly interest rate = Annual rate / 12 / 100
- **n** = Number of months (term)

### Example Calculation

For a £10,000 loan at 12% annual rate for 24 months:
- r = 12 / 12 / 100 = 0.01
- (1+r)^n = (1.01)^24 = 1.2697
- EMI = 10000 × 0.01 × 1.2697 / (1.2697 - 1) = 10000 × 0.012697 / 0.2697 = £470.73

---

## Interface: `IAmortizationService`

```csharp
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for generating amortization schedules using the EMI formula.
/// </summary>
public interface IAmortizationService
{
    /// <summary>
    /// Generates a complete repayment schedule with installments.
    /// </summary>
    RepaymentSchedule GenerateSchedule(
        Guid loanApplicationId,
        Guid lenderId,
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateTime fundingDate);
}
```

---

## Implementation: `AmortizationService.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Features.Funding;

/// <summary>
/// Generates amortization schedules using the standard EMI formula:
/// EMI = P × r × (1+r)^n / ((1+r)^n - 1)
/// where r = annualRate / 12 / 100, n = termMonths.
/// </summary>
public sealed class AmortizationService : IAmortizationService
{
    public RepaymentSchedule GenerateSchedule(
        Guid loanApplicationId,
        Guid lenderId,
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateTime fundingDate)
    {
        // Input validation
        if (principal <= 0)
            throw new DomainException("Principal amount must be greater than zero.");
        if (annualInterestRate <= 0)
            throw new DomainException("Annual interest rate must be greater than zero.");
        if (termMonths <= 0)
            throw new DomainException("Term months must be greater than zero.");

        // Step 1: Calculate monthly rate
        var monthlyRate = annualInterestRate / 12m / 100m;

        // Step 2: Calculate compound factor (1+r)^n
        var compoundFactor = (decimal)Math.Pow((double)(1m + monthlyRate), termMonths);

        // Step 3: Calculate EMI using the formula
        var emi = principal * monthlyRate * compoundFactor / (compoundFactor - 1m);
        emi = decimal.Round(emi, 2);

        // Step 4: Calculate total interest
        var totalInterest = (emi * termMonths) - principal;

        // Step 5: Create the schedule entity
        var schedule = new RepaymentSchedule(
            loanApplicationId, lenderId, principal,
            annualInterestRate, termMonths, emi,
            decimal.Round(totalInterest, 2));

        // Step 6: Generate individual installments
        var remainingBalance = principal;

        for (var i = 1; i <= termMonths; i++)
        {
            // Interest portion = remaining balance × monthly rate
            var interestPortion = decimal.Round(remainingBalance * monthlyRate, 2);

            decimal principalPortion;
            if (i == termMonths)
            {
                // ROUNDING CORRECTION: Final installment absorbs any difference
                principalPortion = remainingBalance;
            }
            else
            {
                principalPortion = decimal.Round(emi - interestPortion, 2);
            }

            remainingBalance -= principalPortion;

            var dueDate = fundingDate.AddMonths(i);

            var installment = new Installment(
                installmentNumber: i,
                dueDate: dueDate,
                principalPortion: principalPortion,
                interestPortion: interestPortion,
                remainingBalance: Math.Max(remainingBalance, 0));

            schedule.AddInstallment(installment);
        }

        return schedule;
    }
}
```

### Step-by-Step Walkthrough

1. **Validate inputs** — Principal, rate, and term must all be positive
2. **Monthly rate** — Convert annual percentage to monthly decimal (e.g., 12% → 0.01)
3. **Compound factor** — `(1 + 0.01)^24` using `Math.Pow`
4. **EMI calculation** — Apply the formula, round to 2 decimal places
5. **Total interest** — `(EMI × months) - principal`
6. **Create schedule** — Instantiate `RepaymentSchedule` with summary data
7. **Generate installments** — Loop through each month:
   - Calculate interest on remaining balance
   - Principal = EMI - interest (except final month)
   - **Final month**: principal = remaining balance (absorbs rounding)
   - Due date = funding date + i months
   - Create `Installment` with internal constructor

### Why the Rounding Correction Matters

Due to `decimal.Round(emi, 2)`, tiny rounding errors accumulate over many months. Without correction, the final remaining balance might be £0.01 or -£0.01 instead of exactly £0.00. The fix: the last installment's principal portion is set to whatever balance remains, ensuring the loan is fully repaid.

---

## RepaymentSchedule Entity

```csharp
public sealed class RepaymentSchedule : AuditableEntity
{
    private readonly List<Installment> _installments = [];

    public RepaymentSchedule(
        Guid loanApplicationId, Guid lenderId,
        decimal fundedAmount, decimal annualInterestRate,
        int termMonths, decimal monthlyEmi, decimal totalInterestPayable)
    {
        LoanApplicationId = loanApplicationId;
        LenderId = lenderId;
        FundedAmount = fundedAmount;
        AnnualInterestRate = annualInterestRate;
        TermMonths = termMonths;
        MonthlyEmi = monthlyEmi;
        TotalInterestPayable = totalInterestPayable;
        Performance = LoanPerformance.OnTime;
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public Guid LoanApplicationId { get; private set; }
    public Guid LenderId { get; private set; }
    public decimal FundedAmount { get; private set; }
    public decimal AnnualInterestRate { get; private set; }
    public int TermMonths { get; private set; }
    public decimal MonthlyEmi { get; private set; }
    public decimal TotalInterestPayable { get; private set; }
    public LoanPerformance Performance { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    public void AddInstallment(Installment installment)
    {
        _installments.Add(installment);
    }

    public Installment? GetNextPendingInstallment()
    {
        return _installments
            .Where(i => i.Status is InstallmentStatus.Pending
                or InstallmentStatus.PartiallyPaid
                or InstallmentStatus.Late
                or InstallmentStatus.Missed)
            .OrderBy(i => i.InstallmentNumber)
            .FirstOrDefault();
    }

    public decimal GetTotalPaidToDate()
    {
        return _installments.Sum(i => i.PaidAmount);
    }

    public void UpdatePerformance()
    {
        var orderedInstallments = _installments
            .OrderByDescending(i => i.InstallmentNumber).ToList();

        var consecutiveBad = 0;
        foreach (var installment in orderedInstallments)
        {
            if (installment.Status is InstallmentStatus.Late or InstallmentStatus.Missed)
                consecutiveBad++;
            else
                break;
        }

        if (consecutiveBad >= 3)
            Performance = LoanPerformance.Defaulted;
        else if (_installments.Any(i => i.Status is InstallmentStatus.Late or InstallmentStatus.Missed))
            Performance = LoanPerformance.Late;
        else
            Performance = LoanPerformance.OnTime;

        MarkUpdated();
    }

    public void Restructure(decimal newRate, int newTermMonths, decimal newEmi, decimal newTotalInterest)
    {
        if (Performance == LoanPerformance.OnTime)
            throw new DomainException(
                "Cannot restructure a loan that is performing on time.");

        AnnualInterestRate = newRate;
        TermMonths = newTermMonths;
        MonthlyEmi = newEmi;
        TotalInterestPayable = newTotalInterest;
        Performance = LoanPerformance.OnTime; // Reset after restructuring
        MarkUpdated();
    }

    public void ClearInstallments()
    {
        _installments.Clear();
    }
}
```

### Method Explanations

| Method | Purpose |
|---|---|
| `AddInstallment` | Adds a generated installment to the collection |
| `GetNextPendingInstallment` | Finds the next unpaid installment (for sequential payment) |
| `GetTotalPaidToDate` | Sum of all paid amounts across installments |
| `UpdatePerformance` | Recalculates performance based on consecutive bad installments |
| `Restructure` | Updates terms for distressed loans (guard: must not be OnTime) |
| `ClearInstallments` | Removes all installments (used during restructuring) |

---

## Installment Entity

```csharp
public sealed class Installment : AuditableEntity
{
    internal Installment(
        int installmentNumber, DateTime dueDate,
        decimal principalPortion, decimal interestPortion,
        decimal remainingBalance)
    {
        InstallmentNumber = installmentNumber;
        DueDate = dueDate;
        PrincipalPortion = principalPortion;
        InterestPortion = interestPortion;
        TotalAmount = principalPortion + interestPortion;
        RemainingBalance = remainingBalance;
        Status = InstallmentStatus.Pending;
        PaidAmount = 0;
        LateFeeAmount = 0;
    }

    public int InstallmentNumber { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal PrincipalPortion { get; private set; }
    public decimal InterestPortion { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public InstallmentStatus Status { get; private set; }
    public decimal PaidAmount { get; private set; }
    public DateTime? PaidDate { get; private set; }
    public decimal LateFeeAmount { get; private set; }
}
```

**Key**: The constructor is `internal` — only code within the same assembly (the Domain project or the Application project via `InternalsVisibleTo`) can create installments. This ensures installments are only created through the `AmortizationService`.

---

## Credit Tier Rate Adjustment

The effective rate applied to a loan depends on the borrower's credit tier:

| Credit Tier | Rate Adjustment | Example (8% base) |
|---|---|---|
| A (Excellent) | +0% | 8.00% |
| B (Good) | +2% | 10.00% |
| C (Fair) | +4% | 12.00% |

This adjustment is applied in both:
- `ProductMatchingService` (showing borrowers their effective rate)
- `FundLoanCommandHandler` (calculating the actual funded rate)

---

## Step-by-Step Guide: Adding Variable Rate Support

1. **Domain** — Add `IsVariableRate` and `RateReviewPeriodMonths` to `LoanProduct`
2. **AmortizationService** — Add overload that accepts a rate schedule
3. **Installment** — Add `AppliedRate` property to track rate per installment
4. **Background Service** — Create `RateReviewService` that recalculates EMI periodically
5. **Notification** — Alert borrowers when their rate changes


---

## Deep Dive: How Installments Are Stored

### EF Core Configuration

```csharp
// InstallmentConfiguration.cs
public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("Installments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InstallmentNumber).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.PrincipalPortion).HasPrecision(18, 2);
        builder.Property(x => x.InterestPortion).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.RemainingBalance).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.LateFeeAmount).HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
```

### RepaymentSchedule Configuration

```csharp
// RepaymentScheduleConfiguration.cs
public class RepaymentScheduleConfiguration : IEntityTypeConfiguration<RepaymentSchedule>
{
    public void Configure(EntityTypeBuilder<RepaymentSchedule> builder)
    {
        builder.ToTable("RepaymentSchedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FundedAmount).HasPrecision(18, 2);
        builder.Property(x => x.AnnualInterestRate).HasPrecision(5, 2);
        builder.Property(x => x.MonthlyEmi).HasPrecision(18, 2);
        builder.Property(x => x.TotalInterestPayable).HasPrecision(18, 2);

        builder.Property(x => x.Performance)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(x => x.Installments)
            .WithOne()
            .HasForeignKey(x => x.RepaymentScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LoanApplication)
            .WithMany()
            .HasForeignKey(x => x.LoanApplicationId);

        builder.HasOne(x => x.Lender)
            .WithMany()
            .HasForeignKey(x => x.LenderId);
    }
}
```

---

## Worked Example: Full Schedule Generation

**Input**: £10,000 loan, 10% annual rate, 12 months, funded 2024-01-15

**Calculation**:
- Monthly rate: 10 / 12 / 100 = 0.008333
- Compound factor: (1.008333)^12 = 1.10471
- EMI: 10000 × 0.008333 × 1.10471 / (1.10471 - 1) = £879.16

**Generated Installments**:

| # | Due Date | Principal | Interest | Total | Balance |
|---|---|---|---|---|---|
| 1 | 2024-02-15 | £795.83 | £83.33 | £879.16 | £9,204.17 |
| 2 | 2024-03-15 | £802.46 | £76.70 | £879.16 | £8,401.71 |
| 3 | 2024-04-15 | £809.15 | £70.01 | £879.16 | £7,592.56 |
| 4 | 2024-05-15 | £815.89 | £63.27 | £879.16 | £6,776.67 |
| 5 | 2024-06-15 | £822.69 | £56.47 | £879.16 | £5,953.98 |
| 6 | 2024-07-15 | £829.55 | £49.62 | £879.16 | £5,124.43 |
| 7 | 2024-08-15 | £836.46 | £42.70 | £879.16 | £4,287.97 |
| 8 | 2024-09-15 | £843.43 | £35.73 | £879.16 | £3,444.54 |
| 9 | 2024-10-15 | £850.46 | £28.70 | £879.16 | £2,594.08 |
| 10 | 2024-11-15 | £857.55 | £21.62 | £879.16 | £1,736.53 |
| 11 | 2024-12-15 | £864.69 | £14.47 | £879.16 | £871.84 |
| 12 | 2025-01-15 | £871.84 | £7.27 | £879.11 | £0.00 |

**Note**: Installment #12 has a slightly different total (£879.11 vs £879.16) because the final installment's principal is set to the exact remaining balance, absorbing the £0.05 rounding difference.

---

## LoanPerformance Enum

```csharp
namespace LoanSuperMarket.Domain.Enums;

public enum LoanPerformance
{
    OnTime,    // All installments paid on time
    Late,      // At least one late/missed installment
    Defaulted  // 3+ consecutive late/missed installments
}
```

### Performance Update Algorithm

The `UpdatePerformance()` method works by:
1. Ordering installments by number (descending — most recent first)
2. Counting consecutive "bad" installments from the end
3. If 3+ consecutive → Defaulted
4. If any bad but < 3 consecutive → Late
5. Otherwise → OnTime

This means a borrower can recover from `Late` to `OnTime` by paying all overdue installments.

---

## Integration Points

The amortization engine connects to:

1. **FundLoanCommandHandler** — Generates schedule when loan is funded
2. **RestructureLoanCommandHandler** — Regenerates schedule with new terms
3. **PaymentProcessor** — Uses `GetNextPendingInstallment()` for sequential payment
4. **LatePaymentService** — Iterates installments to detect overdue/missed/defaults
5. **RepaymentSchedule.razor** — Displays the full schedule to borrowers/lenders
6. **CSV Export** — Exports installment data for external use

---

## Step-by-Step Guide: Adding Balloon Payment Support

To support loans where the final payment is larger:

1. **AmortizationService** — Add `GenerateBalloonSchedule()` method:
```csharp
public RepaymentSchedule GenerateBalloonSchedule(
    Guid applicationId, Guid lenderId,
    decimal principal, decimal annualRate,
    int termMonths, decimal balloonPercentage,
    DateTime fundingDate)
{
    var balloonAmount = principal * balloonPercentage;
    var amortizedPrincipal = principal - balloonAmount;
    // Generate normal schedule for amortizedPrincipal
    // Add final installment with balloonAmount as extra principal
}
```

2. **Domain** — Add `IsBalloonPayment` flag to `RepaymentSchedule`
3. **LoanProduct** — Add `SupportsBalloonPayment` option
4. **Blazor** — Show balloon payment indicator in schedule view
