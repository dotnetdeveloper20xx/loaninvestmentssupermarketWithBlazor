# 18 — Lender Funding Engine

## Feature Requirements

The funding engine allows lenders to fund approved loan applications. Key requirements:

1. **Funding Queue**: Shows approved applications matching the lender's products
2. **Fund Loan**: Validates funds, deducts capital, marks funded, generates amortization schedule, audits, notifies
3. **Decline Funding**: Lender can decline with a reason
4. **Top Up Funds**: Lender can add capital to their account
5. **Restructure Loan**: Distressed loans can be restructured with new terms
6. **Real-Time Updates**: SignalR notifications when funding queue changes

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| CQRS | Separate query (queue) and commands (fund/decline/top-up/restructure) |
| Domain Service | `IAmortizationService` generates repayment schedules |
| Audit Trail | Every funding action is logged |
| SignalR | Real-time notifications via `IRealTimeNotifier` |
| Credit Tier Adjustment | Effective rate = base rate + tier premium |

---

## API Layer: `FundingController.cs`

```csharp
[ApiController]
[Route("api/funding")]
[Authorize(Policy = "CanManageProducts")]
public sealed class FundingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILenderRepository _lenderRepository;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FundingQueueItemDto>>>> GetFundingQueue(
        [FromQuery] string? productTitle,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        CancellationToken cancellationToken)
    {
        var query = new GetFundingQueueQuery
        {
            ProductTitleFilter = productTitle,
            MinAmount = minAmount,
            MaxAmount = maxAmount
        };
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{applicationId:guid}/details")]
    public async Task<ActionResult<ApiResponse<FundingApplicationDetailDto>>> GetApplicationDetails(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetFundingApplicationDetailsQuery(applicationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{applicationId:guid}/accept")]
    public async Task<ActionResult<ApiResponse<FundingResultDto>>> AcceptFunding(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);
        if (lenderId is null)
            return Ok(ApiResponse<FundingResultDto>.Fail("No lender profile found."));

        var command = new FundLoanCommand(applicationId, lenderId.Value);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{applicationId:guid}/decline")]
    public async Task<ActionResult<ApiResponse<string>>> DeclineFunding(
        Guid applicationId, [FromBody] DeclineFundingRequest request,
        CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);
        if (lenderId is null)
            return Ok(ApiResponse<string>.Fail("No lender profile found."));

        var command = new DeclineFundingCommand(applicationId, lenderId.Value, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("top-up")]
    public async Task<ActionResult<ApiResponse<decimal>>> TopUpFunds(
        [FromBody] TopUpFundsRequest request, CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);
        if (lenderId is null)
            return Ok(ApiResponse<decimal>.Fail("No lender profile found."));

        var command = new TopUpFundsCommand(lenderId.Value, request.Amount);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{scheduleId:guid}/restructure")]
    public async Task<ActionResult<ApiResponse<RestructureResultDto>>> RestructureLoan(
        Guid scheduleId, [FromBody] RestructureLoanRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RestructureLoanCommand(
            scheduleId, request.NewAnnualRate, request.NewTermMonths, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    private async Task<Guid?> GetCurrentLenderIdAsync(CancellationToken ct)
    {
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
            return null;
        var lender = await _lenderRepository.GetByUserIdAsync(_currentUserService.UserId, ct);
        return lender?.Id;
    }
}
```

---

## FundLoanCommand & Handler

### Command

```csharp
public sealed record FundLoanCommand(
    Guid ApplicationId,
    Guid LenderId) : IRequest<ApiResponse<FundingResultDto>>, ILoanFundingCommand;
```

### Handler — The Core Funding Logic

```csharp
public sealed class FundLoanCommandHandler
    : IRequestHandler<FundLoanCommand, ApiResponse<FundingResultDto>>
{
    private readonly ILenderRepository _lenderRepository;
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly IAmortizationService _amortizationService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRealTimeNotifier _realTimeNotifier;

    public async Task<ApiResponse<FundingResultDto>> Handle(
        FundLoanCommand request, CancellationToken cancellationToken)
    {
        // 1. Load lender
        var lender = await _lenderRepository.GetByIdAsync(request.LenderId, cancellationToken)
            ?? throw new DomainException("Lender not found.");

        // 2. Load application
        var application = await _loanApplicationRepository.GetByIdAsync(
            request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application not found.");

        if (application.LoanProductId is null)
            throw new DomainException("Loan application does not have a product selected.");

        // 3. Load product for interest rate
        var product = await _loanProductRepository.GetByIdAsync(
            application.LoanProductId.Value, cancellationToken)
            ?? throw new DomainException("Loan product not found.");

        // 4. Get borrower for credit tier
        var borrower = await _borrowerRepository.GetByIdAsync(
            application.BorrowerId, cancellationToken);

        // 5. Calculate effective rate (base + credit tier adjustment)
        var baseRate = product.InterestRate.Percentage;
        var effectiveRate = CalculateEffectiveRate(baseRate, borrower?.CreditTier);

        var fundingAmount = application.RequestedAmount.Amount;

        // 6. Deduct funds from lender (domain guard: insufficient funds throws)
        lender.DeductFunds(fundingAmount);

        // 7. Mark application as funded (domain guard: must be Approved)
        application.Fund();

        // 8. Generate amortization schedule
        var schedule = _amortizationService.GenerateSchedule(
            application.Id, lender.Id, fundingAmount,
            effectiveRate, application.TermMonths, DateTime.UtcNow);

        // 9. Persist schedule
        await _loanApplicationRepository.AddRepaymentScheduleAsync(schedule, cancellationToken);

        // 10. Audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create("LoanApplication", application.Id, "Funded",
                $"Loan funded by {lender.CompanyName}. EMI: {schedule.MonthlyEmi:N2}"),
            cancellationToken);

        // 11. Save all changes
        await _lenderRepository.SaveChangesAsync(cancellationToken);

        // 12. Real-time notifications
        await _realTimeNotifier.NotifyFundingQueueChangedAsync(cancellationToken);
        if (borrower?.UserId is not null)
            await _realTimeNotifier.NotifyLoanFundedAsync(
                borrower.UserId, application.Id, fundingAmount, cancellationToken);

        return ApiResponse<FundingResultDto>.Ok(new FundingResultDto
        {
            ScheduleId = schedule.Id,
            MonthlyEmi = schedule.MonthlyEmi,
            TotalInterest = schedule.TotalInterestPayable,
            TermMonths = schedule.TermMonths,
            FundedAmount = schedule.FundedAmount,
            EffectiveRate = effectiveRate
        }, "Loan funded successfully.");
    }

    private static decimal CalculateEffectiveRate(decimal baseRate, CreditTier? creditTier)
    {
        return creditTier switch
        {
            CreditTier.A => baseRate,
            CreditTier.B => baseRate + 2m,
            CreditTier.C => baseRate + 4m,
            _ => baseRate
        };
    }
}
```

### Funding Flow (12 Steps)

1. Load lender entity
2. Load loan application
3. Load associated loan product (for base interest rate)
4. Load borrower (for credit tier)
5. Calculate effective rate = base rate + tier adjustment
6. Deduct funds from lender (`DeductFunds` throws if insufficient)
7. Mark application as funded (`Fund()` throws if not Approved)
8. Generate full amortization schedule with installments
9. Persist the schedule to database
10. Create audit log entry
11. Save all changes in single transaction
12. Push real-time notifications via SignalR

---

## Blazor Frontend: `FundingQueue.razor`

The funding queue page shows:
- Available capital banner
- Filter controls (product title, min/max amount)
- Table of approved applications with borrower, credit tier, amount, term, product, rate
- "View Details" button opens `FundingDecision.razor` component

Key features:
- Displays credit tier badges (A=green, B=blue, C=amber)
- Shows effective rate (already adjusted for credit tier)
- "Pay All Remaining" bulk payment option
- Top-up funds modal
- Restructure modal for distressed loans

---

## Step-by-Step Guide: Adding Partial Funding

To allow multiple lenders to partially fund a single application:

1. **Domain** — Add `FundedAmount` tracking to `LoanApplication`
2. **Domain** — Modify `Fund()` to accept partial amounts
3. **Application** — Update `FundLoanCommandHandler` to handle partial funding
4. **Infrastructure** — Support multiple `RepaymentSchedule` per application
5. **Blazor** — Show funding progress bar on queue items


---

## Deep Dive: GetFundingQueueQuery

The funding queue shows only applications that:
1. Have status `Approved`
2. Belong to products owned by the current lender
3. Match optional filters (product title, amount range)

### Repository Implementation

```csharp
public async Task<IReadOnlyList<FundingQueueItemDto>> GetFundingQueueAsync(
    string? lenderUserId, string? productTitleFilter,
    decimal? minAmount, decimal? maxAmount, CancellationToken ct)
{
    var query = _context.LoanApplications
        .Where(x => x.Status == LoanApplicationStatus.Approved)
        .AsQueryable();

    // Filter by product title
    if (!string.IsNullOrWhiteSpace(productTitleFilter))
    {
        query = query.Where(x => x.LoanProductId != null
            && _context.LoanProducts
                .Where(p => p.Id == x.LoanProductId)
                .Any(p => p.Title.Contains(productTitleFilter)));
    }

    // Filter by amount range
    if (minAmount.HasValue)
        query = query.Where(x => x.RequestedAmount.Amount >= minAmount.Value);
    if (maxAmount.HasValue)
        query = query.Where(x => x.RequestedAmount.Amount <= maxAmount.Value);

    // Filter by lender's products
    if (!string.IsNullOrWhiteSpace(lenderUserId))
    {
        var lenderIds = await _context.Lenders
            .Where(l => l.UserId == lenderUserId)
            .Select(l => l.Id).ToListAsync(ct);

        if (lenderIds.Count > 0)
        {
            var lenderProductIds = await _context.LoanProducts
                .Where(p => lenderIds.Contains(p.LenderId))
                .Select(p => p.Id).ToListAsync(ct);

            query = query.Where(x => x.LoanProductId != null
                && lenderProductIds.Contains(x.LoanProductId.Value));
        }
    }

    return await query
        .OrderBy(x => x.ReviewedAtUtc)
        .Select(x => new FundingQueueItemDto
        {
            ApplicationId = x.Id,
            BorrowerName = /* lookup */,
            CreditTier = /* lookup */,
            Amount = x.RequestedAmount.Amount,
            TermMonths = x.TermMonths,
            ProductTitle = /* lookup */,
            EffectiveRate = /* lookup */,
            ApprovalDate = x.ReviewedAtUtc ?? x.CreatedAtUtc
        })
        .ToListAsync(ct);
}
```

---

## DeclineFundingCommand

When a lender declines to fund an application:

```csharp
public sealed record DeclineFundingCommand(
    Guid ApplicationId,
    Guid LenderId,
    string Reason) : IRequest<ApiResponse<string>>;
```

The handler:
1. Validates the lender exists
2. Logs the decline with reason in the audit trail
3. The application remains in `Approved` status (other lenders can still fund it)
4. Sends notification to admin about the decline

---

## TopUpFundsCommand

```csharp
public sealed record TopUpFundsCommand(
    Guid LenderId,
    decimal Amount) : IRequest<ApiResponse<decimal>>;
```

Handler:
```csharp
public async Task<ApiResponse<decimal>> Handle(TopUpFundsCommand request, CancellationToken ct)
{
    var lender = await _lenderRepository.GetByIdAsync(request.LenderId, ct)
        ?? throw new DomainException("Lender not found.");

    lender.TopUpFunds(request.Amount); // Domain guard: amount > 0

    await _lenderRepository.SaveChangesAsync(ct);

    return ApiResponse<decimal>.Ok(
        lender.AvailableFunds,
        "Funds topped up successfully.");
}
```

---

## FundingDecision.razor Component

The `FundingDecision` component is shown as a modal/panel when a lender clicks "View Details":

```razor
<div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
    <div class="bg-white rounded-2xl p-8 max-w-lg w-full">
        @if (_isLoading)
        {
            <LoadingSkeleton />
        }
        else if (_detail is not null)
        {
            <h2 class="text-xl font-bold">Funding Decision</h2>

            <div class="mt-4 space-y-3">
                <InfoTile Label="Borrower" Value="@_detail.BorrowerName" />
                <InfoTile Label="Amount" Value="@($"£{_detail.Amount:N0}")" />
                <InfoTile Label="Term" Value="@($"{_detail.TermMonths} months")" />
                <InfoTile Label="Product" Value="@_detail.ProductTitle" />
                <InfoTile Label="Effective Rate" Value="@($"{_detail.EffectiveRate:N2}%")" />
                <InfoTile Label="Credit Tier" Value="@_detail.CreditTier" />
                <InfoTile Label="Monthly EMI (est.)" Value="@($"£{_detail.EstimatedEmi:N2}")" />
            </div>

            <div class="mt-6 flex gap-3">
                <button @onclick="AcceptFunding"
                        disabled="@_isProcessing"
                        class="flex-1 rounded-xl bg-green-600 px-4 py-3 text-white font-semibold">
                    @(_isProcessing ? "Processing..." : "Fund This Loan")
                </button>
                <button @onclick="OpenDeclineForm"
                        class="flex-1 rounded-xl border border-red-300 px-4 py-3 text-red-700 font-semibold">
                    Decline
                </button>
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public Guid ApplicationId { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnFunded { get; set; }
    [Parameter] public EventCallback OnDeclined { get; set; }

    private FundingApplicationDetailDto? _detail;
    private bool _isLoading = true;
    private bool _isProcessing;

    protected override async Task OnInitializedAsync()
    {
        var response = await FundingApiClient.GetApplicationDetailsAsync(ApplicationId);
        _detail = response?.Data;
        _isLoading = false;
    }

    private async Task AcceptFunding()
    {
        _isProcessing = true;
        var response = await FundingApiClient.AcceptFundingAsync(ApplicationId);
        if (response?.Success == true)
            await OnFunded.InvokeAsync();
        _isProcessing = false;
    }
}
```

---

## TopUpFundsModal.razor

```razor
<div class="modal">
    <h3>Top Up Capital</h3>
    <p>Add funds to your lending account.</p>

    <AppCurrencyInput Label="Amount to add"
                      @bind-Value="_amount"
                      Min="100" />

    <button @onclick="TopUp" disabled="@_isProcessing">
        @(_isProcessing ? "Processing..." : "Top Up")
    </button>
</div>

@code {
    private decimal _amount = 10000;
    private bool _isProcessing;

    private async Task TopUp()
    {
        _isProcessing = true;
        var response = await FundingApiClient.TopUpFundsAsync(_amount);
        if (response?.Success == true)
        {
            ToastService.ShowSuccess("Success", $"£{_amount:N0} added to your account.");
            await OnTopUp.InvokeAsync();
        }
        _isProcessing = false;
    }
}
```

---

## Real-Time Notifications

After funding, the handler pushes notifications via SignalR:

```csharp
// Notify all lenders that the queue has changed
await _realTimeNotifier.NotifyFundingQueueChangedAsync(cancellationToken);

// Notify the specific borrower that their loan was funded
if (borrower?.UserId is not null)
{
    await _realTimeNotifier.NotifyLoanFundedAsync(
        borrower.UserId, application.Id, fundingAmount, cancellationToken);
}
```

The Blazor client listens via `LoanHubClient`:
```csharp
_hubConnection.On("FundingQueueChanged", async () =>
{
    await LoadQueueAsync(); // Auto-refresh the queue
    StateHasChanged();
});
```

---

## Step-by-Step Guide: Adding Funding Limits

To add maximum funding per lender per day:

1. **Configuration** — Add `MaxDailyFundingAmount` to settings
2. **Repository** — Add `GetTodaysFundingTotalAsync(Guid lenderId)`
3. **Handler** — Check daily limit before `DeductFunds`:
```csharp
var todayTotal = await _repository.GetTodaysFundingTotalAsync(lender.Id, ct);
if (todayTotal + fundingAmount > settings.MaxDailyFundingAmount)
    throw new DomainException("Daily funding limit exceeded.");
```
4. **Blazor** — Show remaining daily limit on the funding queue page
