# 22 — Loan Restructuring

## Feature Requirements

Loan restructuring allows lenders to modify the terms of distressed loans. Key requirements:

1. **Only Distressed Loans**: Only loans with `Late` or `Defaulted` performance can be restructured
2. **New Terms**: Lender specifies new annual rate and new term (months)
3. **EMI Recalculation**: New EMI is calculated based on remaining principal with new terms
4. **Schedule Regeneration**: Old unpaid installments are cleared and replaced with new ones
5. **Audit Trail**: Every restructuring is logged with reason and new terms
6. **Performance Reset**: After restructuring, performance resets to `OnTime`

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Domain Guard | `RepaymentSchedule.Restructure()` prevents restructuring healthy loans |
| Amortization Service | Recalculates EMI and generates new installments |
| CQRS Command | `RestructureLoanCommand` with handler |
| Audit Logging | Records restructuring details |

---

## Command: `RestructureLoanCommand`

```csharp
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.RestructureLoan;

public sealed record RestructureLoanCommand(
    Guid ScheduleId,
    decimal NewAnnualRate,
    int NewTermMonths,
    string? Reason) : IRequest<ApiResponse<RestructureResultDto>>;
```

### Parameters

| Parameter | Type | Purpose |
|---|---|---|
| `ScheduleId` | `Guid` | The repayment schedule to restructure |
| `NewAnnualRate` | `decimal` | New annual interest rate (e.g., 8.5) |
| `NewTermMonths` | `int` | New term length in months |
| `Reason` | `string?` | Optional reason for restructuring |

---

## Handler: `RestructureLoanCommandHandler`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.RestructureLoan;

public sealed class RestructureLoanCommandHandler
    : IRequestHandler<RestructureLoanCommand, ApiResponse<RestructureResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAmortizationService _amortizationService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RestructureLoanCommandHandler(
        ILoanApplicationRepository repository,
        IAmortizationService amortizationService,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _amortizationService = amortizationService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<RestructureResultDto>> Handle(
        RestructureLoanCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Load the existing schedule with installments
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken)
            ?? throw new DomainException("Repayment schedule not found.");

        // Step 2: Calculate remaining principal (sum of unpaid principal portions)
        var remainingPrincipal = schedule.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .Sum(i => i.PrincipalPortion);

        if (remainingPrincipal <= 0)
            throw new DomainException("Loan is fully paid. Cannot restructure.");

        // Step 3: Generate new schedule with remaining principal and new terms
        var newSchedule = _amortizationService.GenerateSchedule(
            schedule.LoanApplicationId,
            schedule.LenderId,
            remainingPrincipal,
            request.NewAnnualRate,
            request.NewTermMonths,
            DateTime.UtcNow);

        // Step 4: Apply restructuring to existing schedule
        // (Domain guard: throws if Performance == OnTime)
        schedule.Restructure(
            request.NewAnnualRate,
            request.NewTermMonths,
            newSchedule.MonthlyEmi,
            newSchedule.TotalInterestPayable);

        // Step 5: Clear old installments and add new ones
        schedule.ClearInstallments();
        foreach (var installment in newSchedule.Installments)
        {
            schedule.AddInstallment(installment);
        }

        // Step 6: Create audit log entry
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                schedule.LoanApplicationId,
                "Restructured",
                $"Loan restructured: new rate {request.NewAnnualRate:N2}%, " +
                $"new term {request.NewTermMonths} months, " +
                $"new EMI £{newSchedule.MonthlyEmi:N2}. " +
                $"Reason: {request.Reason ?? "Not specified"}"),
            cancellationToken);

        // Step 7: Save all changes
        await _repository.SaveChangesAsync(cancellationToken);

        // Step 8: Return result
        return ApiResponse<RestructureResultDto>.Ok(new RestructureResultDto
        {
            ScheduleId = schedule.Id,
            NewRate = request.NewAnnualRate,
            NewTermMonths = request.NewTermMonths,
            NewMonthlyEmi = newSchedule.MonthlyEmi,
            NewTotalInterest = newSchedule.TotalInterestPayable,
            RemainingInstallments = request.NewTermMonths
        }, "Loan restructured successfully.");
    }
}
```

### Step-by-Step Walkthrough

1. **Load schedule** — Includes all installments via `Include(s => s.Installments)`
2. **Calculate remaining principal** — Only unpaid installments count. This is the new "loan amount" for recalculation.
3. **Generate new schedule** — Uses `AmortizationService` with remaining principal, new rate, new term
4. **Apply restructuring** — `schedule.Restructure()` updates the schedule's metadata and resets performance
5. **Replace installments** — `ClearInstallments()` removes old ones, then new ones are added
6. **Audit** — Records the action with all relevant details
7. **Save** — Single database transaction
8. **Return** — New EMI, rate, term, and total interest

---

## Domain Guard: `RepaymentSchedule.Restructure()`

```csharp
public void Restructure(decimal newRate, int newTermMonths, decimal newEmi, decimal newTotalInterest)
{
    if (Performance == LoanPerformance.OnTime)
        throw new DomainException(
            "Cannot restructure a loan that is performing on time. " +
            "Only late or distressed loans can be restructured.");

    AnnualInterestRate = newRate;
    TermMonths = newTermMonths;
    MonthlyEmi = newEmi;
    TotalInterestPayable = newTotalInterest;

    // Reset performance after restructuring — fresh start
    Performance = LoanPerformance.OnTime;
    MarkUpdated();
}
```

### Why Only Distressed Loans?

Restructuring is a concession — it typically means:
- Lower interest rate (lender earns less)
- Longer term (borrower pays more total interest but lower monthly)
- Both (most common)

It only makes business sense when the alternative is default/write-off. Healthy loans should continue as-is.

---

## API Endpoint

```csharp
[HttpPost("{scheduleId:guid}/restructure")]
public async Task<ActionResult<ApiResponse<RestructureResultDto>>> RestructureLoan(
    Guid scheduleId,
    [FromBody] RestructureLoanRequest request,
    CancellationToken cancellationToken)
{
    var command = new RestructureLoanCommand(
        scheduleId, request.NewAnnualRate, request.NewTermMonths, request.Reason);
    var result = await _sender.Send(command, cancellationToken);
    return Ok(result);
}
```

### Request DTO

```csharp
public sealed class RestructureLoanRequest
{
    public decimal NewAnnualRate { get; set; }
    public int NewTermMonths { get; set; }
    public string? Reason { get; set; }
}
```

---

## Blazor: `RestructureModal.razor`

The restructure modal is shown from the `RepaymentSchedule.razor` page:

```razor
<RestructureModal IsOpen="_showRestructure"
                  ScheduleId="ScheduleId"
                  CurrentRate="_schedule.AnnualInterestRate"
                  CurrentTerm="_schedule.TermMonths"
                  OnClose="CloseRestructure"
                  OnRestructured="HandleRestructured" />
```

The modal:
1. Shows current rate and term
2. Accepts new rate and new term inputs
3. Optionally accepts a reason
4. Calls `FundingApiClient.RestructureLoanAsync()`
5. On success, closes and reloads the schedule

---

## Restructuring Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│ Existing Schedule (Performance: Late or Defaulted)       │
│                                                          │
│ Installments: [Paid, Paid, Late, Missed, Pending, ...]  │
└─────────────────────────┬───────────────────────────────┘
                          │
                          │ RestructureLoanCommand
                          │ (newRate=6%, newTerm=18)
                          ▼
┌─────────────────────────────────────────────────────────┐
│ Step 1: Calculate remaining principal                    │
│   = Sum of PrincipalPortion where Status != Paid        │
│   = e.g., £7,500 remaining                              │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ Step 2: Generate new schedule                            │
│   AmortizationService.GenerateSchedule(                  │
│     principal: £7,500,                                   │
│     rate: 6%,                                            │
│     term: 18 months,                                     │
│     fundingDate: now)                                    │
│   → New EMI: £437.21                                    │
│   → 18 new installments                                 │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│ Step 3: Apply to existing schedule                       │
│   schedule.Restructure(6%, 18, £437.21, totalInterest)  │
│   schedule.ClearInstallments()                           │
│   foreach (inst in newSchedule.Installments)             │
│       schedule.AddInstallment(inst)                      │
│                                                          │
│ Result: Performance = OnTime, fresh installments         │
└─────────────────────────────────────────────────────────┘
```

---

## Audit Trail Entry

After restructuring, an audit log entry is created:

```
Entity: LoanApplication
EntityId: {applicationId}
Action: Restructured
Details: "Loan restructured: new rate 6.00%, new term 18 months, new EMI £437.21. Reason: Borrower financial hardship"
```

This provides a complete history of all restructuring actions for compliance and reporting.

---

## Step-by-Step Guide: Adding Restructuring Limits

To prevent excessive restructuring:

1. **Domain** — Add `RestructureCount` property to `RepaymentSchedule`
2. **Domain** — In `Restructure()`, increment count and check max:
```csharp
if (RestructureCount >= 3)
    throw new DomainException("Maximum restructuring attempts reached.");
RestructureCount++;
```
3. **Shared** — Add `MaxRestructureAttempts` to `RepaymentSettings`
4. **Blazor** — Show remaining restructure attempts in the modal
5. **Audit** — Include restructure count in audit log entry


---

## Deep Dive: Remaining Principal Calculation

The restructuring handler calculates remaining principal by summing unpaid installment principal portions:

```csharp
var remainingPrincipal = schedule.Installments
    .Where(i => i.Status != InstallmentStatus.Paid)
    .Sum(i => i.PrincipalPortion);
```

### Why Not Use `RemainingBalance`?

You might think we could use the `RemainingBalance` of the last paid installment. However:
- `RemainingBalance` on each installment represents the balance *after* that installment is paid
- For partially paid installments, the remaining balance doesn't account for the partial payment
- Summing unpaid principal portions gives the exact amount still owed

### Example

```
Schedule: £10,000 loan, 12 months
Installments 1-4: Paid (principal portions: £795, £802, £809, £816 = £3,222 paid)
Installment 5: Late (principal: £823)
Installments 6-12: Pending (principal: £830 + £837 + ... = £5,955)

Remaining Principal = £823 + £5,955 = £6,778
```

---

## Restructuring Validation

The `RestructureLoanCommandValidator` (FluentValidation) ensures:

```csharp
public sealed class RestructureLoanCommandValidator
    : AbstractValidator<RestructureLoanCommand>
{
    public RestructureLoanCommandValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("Schedule ID is required.");

        RuleFor(x => x.NewAnnualRate)
            .GreaterThan(0)
            .WithMessage("New annual rate must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("New annual rate cannot exceed 100%.");

        RuleFor(x => x.NewTermMonths)
            .GreaterThan(0)
            .WithMessage("New term must be greater than zero.")
            .LessThanOrEqualTo(360)
            .WithMessage("New term cannot exceed 360 months (30 years).");
    }
}
```

---

## Before vs After Restructuring

### Before (Distressed Loan)

```
Schedule: £10,000 at 12%, 12 months
EMI: £888.49
Performance: Late (2 consecutive missed)
Remaining Principal: £6,778
Installments: 4 paid, 2 missed, 6 pending
```

### After Restructuring (New Terms: 8%, 18 months)

```
Schedule: £6,778 at 8%, 18 months
New EMI: £395.12
Performance: OnTime (reset)
Installments: 18 new pending installments
Old installments: Cleared
```

### Impact on Borrower

- **Lower monthly payment**: £888.49 → £395.12 (55% reduction)
- **Longer term**: 6 months remaining → 18 months
- **More total interest**: But avoids default and collections

### Impact on Lender

- **Lower return**: 12% → 8% rate
- **Extended timeline**: Gets money back over 18 months instead of 6
- **Avoids write-off**: Better than 0% recovery on a defaulted loan

---

## Blazor: RestructureModal.razor

```razor
@if (IsOpen)
{
    <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div class="bg-white rounded-2xl p-8 max-w-md w-full">
            <h2 class="text-xl font-bold text-slate-900">Restructure Loan</h2>
            <p class="text-sm text-slate-500 mt-1">
                Modify the loan terms for this distressed loan.
            </p>

            <div class="mt-4 rounded-lg bg-amber-50 border border-amber-200 p-3 text-sm text-amber-700">
                ⚠️ Only distressed loans (Late or Defaulted) can be restructured.
            </div>

            <div class="mt-6 space-y-4">
                <div>
                    <label class="text-sm font-medium text-slate-700">Current Rate</label>
                    <div class="text-lg font-bold text-slate-900">@CurrentRate.ToString("N2")%</div>
                </div>

                <AppPercentageInput Label="New Annual Rate"
                                    @bind-Value="_newRate"
                                    Min="1" Max="50" />

                <div>
                    <label class="text-sm font-medium text-slate-700">Current Term</label>
                    <div class="text-lg font-bold text-slate-900">@CurrentTerm months</div>
                </div>

                <AppNumberInput Label="New Term (months)"
                                @bind-Value="_newTerm"
                                Min="3" Max="360" />

                <AppTextArea Label="Reason (optional)"
                             @bind-Value="_reason"
                             Placeholder="e.g., Borrower financial hardship..." />
            </div>

            <div class="mt-6 flex gap-3">
                <button @onclick="SubmitRestructure" disabled="@_isProcessing"
                        class="flex-1 rounded-xl bg-amber-600 px-4 py-3 text-white font-semibold">
                    @(_isProcessing ? "Restructuring..." : "Restructure Loan")
                </button>
                <button @onclick="OnClose.InvokeAsync"
                        class="flex-1 rounded-xl border px-4 py-3 text-slate-700 font-semibold">
                    Cancel
                </button>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public Guid ScheduleId { get; set; }
    [Parameter] public decimal CurrentRate { get; set; }
    [Parameter] public int CurrentTerm { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnRestructured { get; set; }

    private decimal _newRate;
    private int _newTerm;
    private string _reason = string.Empty;
    private bool _isProcessing;

    protected override void OnParametersSet()
    {
        _newRate = CurrentRate > 2 ? CurrentRate - 2 : CurrentRate;
        _newTerm = CurrentTerm + 6;
    }

    private async Task SubmitRestructure()
    {
        _isProcessing = true;
        try
        {
            var response = await FundingApiClient.RestructureLoanAsync(
                ScheduleId, _newRate, _newTerm, _reason);

            if (response?.Success == true)
            {
                ToastService.ShowSuccess("Restructured",
                    $"Loan restructured: {_newRate:N2}%, {_newTerm} months.");
                await OnRestructured.InvokeAsync();
            }
            else
            {
                ToastService.ShowError("Error",
                    response?.Errors.FirstOrDefault() ?? "Restructuring failed.");
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
```

---

## Audit Trail Integration

The restructuring audit entry is visible in the `LoanAuditTrail` component on the repayment schedule page:

```razor
<LoanAuditTrail EntityName="LoanApplication" EntityId="_schedule.LoanApplicationId" />
```

This component fetches audit logs for the loan application and displays them as a timeline, showing:
- When the loan was funded
- When payments were made
- When the loan was restructured (with new terms)
- When defaults were detected

---

## Step-by-Step Guide: Adding Restructuring Approval Workflow

Currently, lenders can restructure immediately. To add an approval step:

1. **Domain** — Add `RestructureStatus` enum (Proposed, Approved, Applied)
2. **Domain** — Create `RestructureProposal` entity with proposed terms
3. **Application** — Split into `ProposeRestructureCommand` and `ApproveRestructureCommand`
4. **API** — Add endpoints:
   - `POST /api/funding/{scheduleId}/propose-restructure`
   - `POST /api/funding/{scheduleId}/approve-restructure`
5. **Notification** — Notify borrower of proposed restructure for acceptance
6. **Blazor** — Add proposal review UI for borrowers
