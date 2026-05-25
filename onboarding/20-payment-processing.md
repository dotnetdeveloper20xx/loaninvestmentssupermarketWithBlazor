# 20 — Payment Processing

## Feature Requirements

The payment processing system handles borrower repayments. Key requirements:

1. **Single Payment**: Pay the next pending installment (full or partial)
2. **Bulk Payment**: Pay off multiple installments at once (early payoff)
3. **Sequential Enforcement**: Payments must be applied in installment order
4. **Payment History**: Track all payments with dates and amounts
5. **CSV Export**: Export repayment schedule as CSV
6. **Inline Payment UI**: Pay directly from the repayment schedule page

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Domain Service | `IPaymentProcessor` enforces sequential payment rules |
| State Machine | Installment: Pending → PartiallyPaid → Paid |
| CQRS | Commands for payments, queries for history/schedule |
| CSV Generation | Server-side CSV export endpoint |

---

## Domain Service: `IPaymentProcessor`

```csharp
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

/// <summary>
/// Domain service responsible for processing payments against a repayment schedule.
/// Enforces sequential payment order and delegates to installment state machine.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Records a payment against the next pending installment in the schedule.
    /// </summary>
    void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate);

    /// <summary>
    /// Records a bulk payment that pays off multiple installments sequentially.
    /// Returns the number of installments fully paid.
    /// </summary>
    int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate);
}
```

---

## Implementation: `PaymentProcessor.cs`

```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

/// <summary>
/// Domain service that enforces sequential payment order and delegates
/// to the installment entity's state machine methods.
/// </summary>
public sealed class PaymentProcessor : IPaymentProcessor
{
    public void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        var nextInstallment = schedule.GetNextPendingInstallment();
        if (nextInstallment is null)
            throw new DomainException("No pending installments found. All payments are complete.");

        var totalOwed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount
                      - nextInstallment.PaidAmount;

        if (amount > totalOwed)
            throw new DomainException(
                $"Payment of {amount:N2} exceeds the remaining balance of {totalOwed:N2} " +
                $"on installment #{nextInstallment.InstallmentNumber}.");

        if (amount >= totalOwed)
            nextInstallment.RecordFullPayment(paymentDate);
        else
            nextInstallment.RecordPartialPayment(amount, paymentDate);

        schedule.UpdatePerformance();
    }

    public int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate)
    {
        if (totalAmount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        var remaining = totalAmount;
        var installmentsPaid = 0;

        while (remaining > 0)
        {
            var nextInstallment = schedule.GetNextPendingInstallment();
            if (nextInstallment is null)
                break; // All installments paid off

            var owed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount
                     - nextInstallment.PaidAmount;

            if (remaining >= owed)
            {
                nextInstallment.RecordFullPayment(paymentDate);
                remaining -= owed;
                installmentsPaid++;
            }
            else
            {
                nextInstallment.RecordPartialPayment(remaining, paymentDate);
                remaining = 0;
            }
        }

        schedule.UpdatePerformance();
        return installmentsPaid;
    }
}
```

### Sequential Enforcement Explained

The `PaymentProcessor` always calls `schedule.GetNextPendingInstallment()` which returns installments ordered by `InstallmentNumber`. This means:
- You cannot pay installment #5 before #4 is fully paid
- Partial payments accumulate on the current installment
- Once an installment is fully paid, the next one becomes "next pending"

### Installment Payment Methods

```csharp
// On the Installment entity:

public void RecordFullPayment(DateTime paymentDate)
{
    if (Status == InstallmentStatus.Paid)
        throw new DomainException("Installment is already fully paid.");

    var totalOwed = TotalAmount + LateFeeAmount;
    PaidAmount = totalOwed;
    PaidDate = paymentDate;
    Status = InstallmentStatus.Paid;
    MarkUpdated();
}

public void RecordPartialPayment(decimal amount, DateTime paymentDate)
{
    if (amount <= 0)
        throw new DomainException("Payment amount must be greater than zero.");
    if (Status == InstallmentStatus.Paid)
        throw new DomainException("Installment is already fully paid.");

    var totalOwed = TotalAmount + LateFeeAmount;
    var newPaidAmount = PaidAmount + amount;

    if (newPaidAmount > totalOwed)
        throw new DomainException(
            $"Payment would exceed total owed of {totalOwed:N2}.");

    PaidAmount = newPaidAmount;
    PaidDate = paymentDate;

    if (PaidAmount >= totalOwed)
        Status = InstallmentStatus.Paid;
    else
        Status = InstallmentStatus.PartiallyPaid;

    MarkUpdated();
}
```

---

## Application Layer: `RecordPaymentCommand`

```csharp
public sealed record RecordPaymentCommand(
    Guid ScheduleId,
    decimal Amount,
    DateTime PaymentDate) : IRequest<ApiResponse<PaymentResultDto>>;
```

### Handler

```csharp
public sealed class RecordPaymentCommandHandler
    : IRequestHandler<RecordPaymentCommand, ApiResponse<PaymentResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IPaymentProcessor _paymentProcessor;

    public async Task<ApiResponse<PaymentResultDto>> Handle(
        RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken)
            ?? throw new DomainException("Repayment schedule not found.");

        // Delegate to domain service
        _paymentProcessor.RecordPayment(schedule, request.Amount, request.PaymentDate);

        await _repository.SaveChangesAsync(cancellationToken);

        var paidInstallment = schedule.Installments
            .OrderByDescending(i => i.PaidDate).First();

        var totalOwed = paidInstallment.TotalAmount + paidInstallment.LateFeeAmount;

        return ApiResponse<PaymentResultDto>.Ok(new PaymentResultDto
        {
            InstallmentNumber = paidInstallment.InstallmentNumber,
            Status = paidInstallment.Status.ToString(),
            PaidAmount = paidInstallment.PaidAmount,
            RemainingOnInstallment = totalOwed - paidInstallment.PaidAmount,
            TotalPaidToDate = schedule.GetTotalPaidToDate()
        }, "Payment recorded successfully.");
    }
}
```

---

## API Layer: `PaymentsController.cs`

```csharp
[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    [HttpPost("{scheduleId:guid}/pay")]
    public async Task<ActionResult<ApiResponse<PaymentResultDto>>> RecordPayment(
        Guid scheduleId, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var command = new RecordPaymentCommand(scheduleId, request.Amount, request.PaymentDate);
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("{scheduleId:guid}/pay-bulk")]
    public async Task<ActionResult<ApiResponse<BulkPaymentResultDto>>> RecordBulkPayment(
        Guid scheduleId, [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        var command = new RecordBulkPaymentCommand(scheduleId, request.Amount, request.PaymentDate);
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    [HttpGet("{scheduleId:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>> GetPaymentHistory(
        Guid scheduleId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPaymentHistoryQuery(scheduleId), ct);
        return Ok(result);
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<ActionResult<ApiResponse<RepaymentScheduleDto>>> GetRepaymentSchedule(
        Guid scheduleId, CancellationToken ct)
    {
        var query = new GetRepaymentScheduleQuery { ScheduleId = scheduleId };
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{scheduleId:guid}/export")]
    public async Task<IActionResult> ExportScheduleCsv(Guid scheduleId, CancellationToken ct)
    {
        var query = new GetRepaymentScheduleQuery { ScheduleId = scheduleId };
        var result = await _sender.Send(query, ct);

        if (!result.Success || result.Data is null)
            return NotFound();

        var csv = new StringBuilder();
        csv.AppendLine("Installment,Due Date,Principal,Interest,Total,Balance,Status,Paid,Paid Date,Late Fee");

        foreach (var inst in result.Data.Installments)
        {
            csv.AppendLine(string.Join(",",
                inst.InstallmentNumber,
                inst.DueDate.ToString("yyyy-MM-dd"),
                inst.PrincipalPortion.ToString("F2"),
                inst.InterestPortion.ToString("F2"),
                inst.TotalAmount.ToString("F2"),
                inst.RemainingBalance.ToString("F2"),
                inst.Status,
                inst.PaidAmount.ToString("F2"),
                inst.PaidDate?.ToString("yyyy-MM-dd") ?? "",
                inst.LateFeeAmount.ToString("F2")));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"repayment-schedule-{scheduleId:N}.csv");
    }
}
```

---

## Blazor Frontend: `RepaymentSchedule.razor`

The repayment schedule page provides:
- Summary cards (funded amount, rate, term, EMI, total interest)
- Payment timeline visualization
- Installment table with inline "Pay" button on next pending
- "Pay All Remaining" bulk payment button
- CSV export link
- Restructure modal for distressed loans

### Key Interaction: Inline Payment

```razor
@if (isNext)
{
    <button @onclick="() => OpenPaymentForm(inst)">Pay</button>
}
```

Only the next pending installment shows the "Pay" button — enforcing sequential payment visually.

---

## Step-by-Step Guide: Adding Payment Method Tracking

1. **Shared** — Add `PaymentMethod` enum (BankTransfer, Card, DirectDebit)
2. **Domain** — Add `PaymentMethod` property to `Installment`
3. **Application** — Update `RecordPaymentCommand` to include payment method
4. **API** — Update `RecordPaymentRequest` DTO
5. **Blazor** — Add payment method selector to `PaymentForm.razor`


---

## Deep Dive: Bulk Payment (Early Payoff)

The bulk payment feature allows borrowers to pay off multiple installments at once — useful for early loan settlement.

### RecordBulkPaymentCommand

```csharp
public sealed record RecordBulkPaymentCommand(
    Guid ScheduleId,
    decimal Amount,
    DateTime PaymentDate) : IRequest<ApiResponse<BulkPaymentResultDto>>;
```

### Handler

```csharp
public sealed class RecordBulkPaymentCommandHandler
    : IRequestHandler<RecordBulkPaymentCommand, ApiResponse<BulkPaymentResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IPaymentProcessor _paymentProcessor;

    public async Task<ApiResponse<BulkPaymentResultDto>> Handle(
        RecordBulkPaymentCommand request, CancellationToken ct)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(request.ScheduleId, ct)
            ?? throw new DomainException("Repayment schedule not found.");

        var installmentsPaid = _paymentProcessor.RecordBulkPayment(
            schedule, request.Amount, request.PaymentDate);

        await _repository.SaveChangesAsync(ct);

        return ApiResponse<BulkPaymentResultDto>.Ok(new BulkPaymentResultDto
        {
            InstallmentsPaid = installmentsPaid,
            TotalAmountApplied = request.Amount,
            TotalPaidToDate = schedule.GetTotalPaidToDate(),
            RemainingInstallments = schedule.Installments
                .Count(i => i.Status != InstallmentStatus.Paid)
        }, $"{installmentsPaid} installment(s) paid.");
    }
}
```

### How Bulk Payment Works

```
Input: £2,000 bulk payment

Installment #5: Owes £470.73 → Pay full → Remaining: £1,529.27 → installmentsPaid = 1
Installment #6: Owes £470.73 → Pay full → Remaining: £1,058.54 → installmentsPaid = 2
Installment #7: Owes £470.73 → Pay full → Remaining: £587.81 → installmentsPaid = 3
Installment #8: Owes £470.73 → Pay partial (£587.81) → Status: PartiallyPaid

Result: 3 installments fully paid, 1 partially paid
```

---

## PaymentsApiClient (Blazor)

```csharp
public sealed class PaymentsApiClient
{
    private readonly HttpClient _httpClient;

    public PaymentsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<PaymentResultDto>?> RecordPaymentAsync(
        Guid scheduleId, decimal amount, DateTime paymentDate, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/payments/{scheduleId}/pay",
            new RecordPaymentRequest { Amount = amount, PaymentDate = paymentDate }, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentResultDto>>(ct);
    }

    public async Task<ApiResponse<BulkPaymentResultDto>?> RecordBulkPaymentAsync(
        Guid scheduleId, decimal amount, DateTime paymentDate, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/payments/{scheduleId}/pay-bulk",
            new RecordPaymentRequest { Amount = amount, PaymentDate = paymentDate }, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<BulkPaymentResultDto>>(ct);
    }

    public async Task<ApiResponse<RepaymentScheduleDto>?> GetRepaymentScheduleAsync(
        Guid scheduleId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<RepaymentScheduleDto>>(
            $"api/payments/{scheduleId}", ct);
    }

    public async Task<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>?> GetPaymentHistoryAsync(
        Guid scheduleId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>(
            $"api/payments/{scheduleId}/history", ct);
    }
}
```

---

## PaymentForm.razor Component

The inline payment form appears when a borrower clicks "Pay" on an installment:

```razor
<div class="mt-4 rounded-xl border border-green-200 bg-green-50 p-5">
    <h3 class="font-semibold text-green-800">
        Pay Installment #@Installment.InstallmentNumber
    </h3>

    <div class="mt-3 space-y-3">
        <div class="text-sm text-green-700">
            Total owed: £@TotalOwed.ToString("N2")
            @if (Installment.LateFeeAmount > 0)
            {
                <span class="text-red-600">(includes £@Installment.LateFeeAmount.ToString("N2") late fee)</span>
            }
        </div>

        <div class="flex gap-3">
            <input type="number" @bind="_paymentAmount" step="0.01"
                   min="0.01" max="@TotalOwed"
                   class="rounded-lg border px-3 py-2 w-40" />

            <button @onclick="PayFull"
                    class="rounded-lg bg-green-600 px-4 py-2 text-white text-sm font-semibold">
                Pay Full (£@TotalOwed.ToString("N2"))
            </button>
        </div>

        <div class="flex gap-2">
            <button @onclick="SubmitPayment" disabled="@_isProcessing"
                    class="rounded-lg bg-blue-600 px-4 py-2 text-white text-sm">
                @(_isProcessing ? "Processing..." : "Submit Payment")
            </button>
            <button @onclick="OnClose.InvokeAsync"
                    class="rounded-lg border px-4 py-2 text-sm">
                Cancel
            </button>
        </div>
    </div>
</div>

@code {
    [Parameter] public InstallmentDto Installment { get; set; } = null!;
    [Parameter] public Guid ScheduleId { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnPaymentRecorded { get; set; }

    private decimal _paymentAmount;
    private bool _isProcessing;

    private decimal TotalOwed =>
        Installment.TotalAmount + Installment.LateFeeAmount - Installment.PaidAmount;

    protected override void OnInitialized()
    {
        _paymentAmount = TotalOwed; // Default to full payment
    }

    private void PayFull() => _paymentAmount = TotalOwed;

    private async Task SubmitPayment()
    {
        _isProcessing = true;
        var response = await PaymentsApiClient.RecordPaymentAsync(
            ScheduleId, _paymentAmount, DateTime.UtcNow);

        if (response?.Success == true)
            await OnPaymentRecorded.InvokeAsync();
        _isProcessing = false;
    }
}
```

---

## CSV Export Details

The export endpoint generates a CSV file with all installment data:

```
Installment,Due Date,Principal,Interest,Total,Remaining Balance,Status,Paid Amount,Paid Date,Late Fee
1,2024-02-15,795.83,83.33,879.16,9204.17,Paid,879.16,2024-02-14,0.00
2,2024-03-15,802.46,76.70,879.16,8401.71,Paid,879.16,2024-03-13,0.00
3,2024-04-15,809.15,70.01,879.16,7592.56,Late,0.00,,17.58
...
```

The CSV is generated server-side and returned as a file download with `Content-Type: text/csv`.

---

## Step-by-Step Guide: Adding Payment Receipts

1. **Domain** — Create `PaymentReceipt` entity with receipt number, amount, date
2. **Application** — Generate receipt in `RecordPaymentCommandHandler` after successful payment
3. **API** — Add `GET /api/payments/{scheduleId}/receipts/{receiptId}` endpoint
4. **Blazor** — Add "Download Receipt" button next to paid installments
5. **PDF Generation** — Use a library like QuestPDF to generate formatted receipts
