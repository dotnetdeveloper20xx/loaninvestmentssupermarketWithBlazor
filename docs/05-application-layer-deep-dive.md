# Application Layer — The Complete Bible

## Introduction

The Application layer is the ORCHESTRATOR. It doesn't know business rules
(that's the Domain). It doesn't know how to talk to databases (that's
Infrastructure). It knows HOW TO COORDINATE a business operation from start
to finish.

Location: `src/LoanSuperMarket.Application/`

---

## The CQRS Pattern In Practice

Every operation is either a Command or a Query:

**Commands** (change state):
- `FundLoanCommand` → Funds a loan, deducts capital, generates schedule
- `RecordPaymentCommand` → Records a payment against an installment
- `DeclineFundingCommand` → Declines a funding request
- `TopUpFundsCommand` → Adds capital to a lender
- `RestructureLoanCommand` → Restructures a distressed loan
- `UpdateNotificationPreferencesCommand` → Saves user preferences

**Queries** (read state):
- `GetFundingQueueQuery` → Lists approved applications for funding
- `GetRepaymentScheduleQuery` → Gets a schedule with installments
- `GetLenderDashboardQuery` → Portfolio KPIs
- `GetInvestorAnalyticsQuery` → ROI and yield calculations
- `GetBorrowerLoansQuery` → Borrower's active loans
- `GetCollectionsQuery` → Defaulted loans for admin

---

## The MediatR Pipeline

When ANY command or query is sent via `_sender.Send(command)`, it passes
through 7 pipeline behaviours IN ORDER before reaching its handler:

```
1. LoggingBehaviour      → Logs the request type and timing
2. PerformanceBehaviour  → Warns if handler takes > 500ms
3. ValidationBehaviour   → Runs FluentValidation, rejects if invalid
4. CachingBehaviour      → Returns cached result for ICacheableQuery
5. AccountStatusBehaviour → Blocks suspended/blocked users
6. LimitEnforcementBehaviour → Checks credit/capital limits
7. ResourceAuthorizationBehaviour → Scopes data by user role
```

Then the HANDLER executes. Then the response flows back.

---

## Key Handler: `FundLoanCommandHandler`

This is the most complex handler. Let me walk through every step:

```
Input: FundLoanCommand(ApplicationId, LenderId)
Output: ApiResponse<FundingResultDto>
```

**Step 1:** Load the lender by ID. If not found → throw DomainException.

**Step 2:** Load the loan application. If not found → throw.

**Step 3:** Verify the application has a product selected. If null → throw.

**Step 4:** Load the loan product to get the base interest rate.

**Step 5:** Load the borrower to get their credit tier.

**Step 6:** Calculate effective rate:
- Tier A → base rate (no change)
- Tier B → base rate + 2%
- Tier C → base rate + 4%
- No tier → base rate

**Step 7:** Get the funding amount from the application's RequestedAmount.

**Step 8:** Call `lender.DeductFunds(fundingAmount)` — reduces capital.

**Step 9:** Call `application.Fund()` — transitions status to Funded.

**Step 10:** Call `amortizationService.GenerateSchedule(...)` — creates the
full repayment plan with all installments.

**Step 11:** Persist the schedule via repository.

**Step 12:** Create an audit log entry recording the funding event.

**Step 13:** Save all changes atomically (single DB transaction).

**Step 14:** Publish domain events (SignalR notifications).

**Step 15:** Return the result DTO with schedule ID, EMI, total interest.

If ANY step fails, the entire transaction rolls back. Nothing is half-done.

---

## The AmortizationService

Location: `Features/Funding/AmortizationService.cs`

This service generates repayment schedules using the EMI formula.

### The EMI Formula

```
EMI = P × r × (1+r)^n / ((1+r)^n - 1)

Where:
  P = principal (funded amount)
  r = monthly interest rate = annual rate / 12 / 100
  n = number of months (term)
```

### Example Calculation

Loan: £10,000 at 12% annual for 12 months

```
r = 12 / 12 / 100 = 0.01
(1+r)^n = (1.01)^12 = 1.1268
EMI = 10000 × 0.01 × 1.1268 / (1.1268 - 1)
EMI = 112.68 / 0.1268
EMI = £888.49
```

### Installment Generation Loop

For each month (1 to n):
1. Interest = remaining balance × monthly rate
2. Principal = EMI - interest
3. New balance = old balance - principal
4. Create installment with these values

**Final installment adjustment:** Due to rounding (2 decimal places), the
sum of all principal portions might not exactly equal the funded amount.
The final installment's principal is set to whatever balance remains,
ensuring the math is perfect.

---

## The LatePaymentService

Location: `Features/Payments/LateDetection/LatePaymentService.cs`

This is NOT a MediatR handler — it's a plain service called by the
background hosted service.

### `ProcessOverdueInstallmentsAsync()`

1. Get all active (non-defaulted) schedules with installments
2. For each installment that is Pending or PartiallyPaid:
   - Calculate overdue date = DueDate + GracePeriodDays (5)
   - If today > overdue date → mark it Late
   - If late notice not yet sent → send notification, mark sent
3. Update each schedule's performance
4. Save all changes

### `ProcessMissedInstallmentsAsync()`

1. For each schedule, order installments by number
2. For each pair (current, next):
   - If current is Late AND today >= next's due date → mark Missed
3. Update performance, save

### `DetectDefaultsAsync()`

1. For each non-defaulted schedule:
   - Count max consecutive Late/Missed
   - If >= 3 → update performance (triggers Defaulted)
   - Send default notice to lender
2. Save

### `SendUpcomingRemindersAsync()`

1. For each Pending installment:
   - If reminder not sent AND today >= (DueDate - 3 days) → send reminder
   - Mark reminder sent
2. Save

---

## Pipeline Behaviours In Detail

### `CachingBehaviour`

```csharp
if (request is not ICacheableQuery cacheableQuery)
    return await next(cancellationToken);  // Skip non-cacheable

if (_cache.TryGetValue(cacheKey, out cached))
    return cached;  // Cache HIT

var response = await next(cancellationToken);  // Cache MISS
_cache.Set(cacheKey, response, expiration);
return response;
```

Currently, `GetLenderDashboardQuery` implements `ICacheableQuery` with a
2-minute cache. This means the portfolio dashboard doesn't hit the database
on every page load — only every 2 minutes.

### `ResourceAuthorizationBehaviour`

For queries implementing `IResourceFilteredQuery`:
- If user is a Borrower → sets FilterByUserId to their ID
- If user is a Lender → sets FilterByUserId to their ID
- If user is Admin → leaves filters null (sees everything)

The handler then uses these filters to scope its database query.
