# 25 — Borrower Dashboard

## Overview

The Borrower Dashboard is the primary interface for loan applicants. It provides a tabbed view of their applications (with status tracking), active loans, payment history, upcoming payments, and a loan calculator. The page also allows borrowers to continue draft applications or upload requested documents.

---

## Feature Requirements (Plain English)

1. Show all borrower's loan applications with status badges (Draft, Submitted, Under Review, Approved, Funded, Rejected, Withdrawn, Documents Requested).
2. Allow continuing draft applications or uploading documents for "Documents Requested" status.
3. Display active loans with remaining balance, next payment date, and progress bar.
4. Show payment history with timeline visualization.
5. List upcoming payments with due dates and amounts.
6. Provide a loan calculator widget for estimating monthly payments.
7. Include an affordability check tool.
8. Only accessible to `Borrower` role.

---

## Technologies & Patterns

| Layer | Technology | Pattern |
|-------|-----------|---------|
| Application | MediatR queries | CQRS read-side, user-scoped |
| Infrastructure | EF Core | Repository with user filtering |
| API | ASP.NET Controller | Bearer-authenticated endpoints |
| Frontend | Blazor WASM | Tab navigation, child components |

---

## Application Layer Queries

### GetBorrowerApplicationsQuery

```csharp
// src/LoanSuperMarket.Application/Features/LoanApplications/GetBorrowerApplications/GetBorrowerApplicationsQuery.cs
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetBorrowerApplications;

public sealed record GetBorrowerApplicationsQuery
    : IRequest<IReadOnlyList<WizardApplicationSummaryDto>>;
```

### GetBorrowerLoansQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetBorrowerLoans/GetBorrowerLoansQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerLoans;

public sealed record GetBorrowerLoansQuery
    : IRequest<IReadOnlyList<BorrowerLoanDto>>;
```

### GetBorrowerPaymentSummaryQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetBorrowerPaymentSummary/GetBorrowerPaymentSummaryQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerPaymentSummary;

public sealed record GetBorrowerPaymentSummaryQuery
    : IRequest<BorrowerPaymentSummaryDto>;
```

---

## Shared DTOs

```csharp
// src/LoanSuperMarket.Shared/LoanApplications/WizardApplicationSummaryDto.cs
namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class WizardApplicationSummaryDto
{
    public Guid ApplicationId { get; init; }
    public string? ProductTitle { get; init; }
    public decimal RequestedAmount { get; init; }
    public int TermMonths { get; init; }
    public int Status { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public int UploadedDocuments { get; init; }
    public int VerifiedDocuments { get; init; }
    public int RejectedDocuments { get; init; }
}
```

```csharp
// src/LoanSuperMarket.Shared/Dashboard/BorrowerLoanDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class BorrowerLoanDto
{
    public Guid LoanId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string LenderName { get; init; } = string.Empty;
    public decimal OriginalAmount { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal MonthlyPayment { get; init; }
    public decimal InterestRate { get; init; }
    public int TotalInstallments { get; init; }
    public int PaidInstallments { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
```

```csharp
// src/LoanSuperMarket.Shared/Dashboard/BorrowerPaymentSummaryDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class BorrowerPaymentSummaryDto
{
    public decimal TotalPaidToDate { get; init; }
    public decimal TotalRemaining { get; init; }
    public int UpcomingPaymentsCount { get; init; }
    public decimal NextPaymentAmount { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public List<UpcomingPaymentDto> UpcomingPayments { get; init; } = [];
    public List<PaymentHistoryItemDto> RecentPayments { get; init; } = [];
}

public sealed class UpcomingPaymentDto
{
    public Guid InstallmentId { get; init; }
    public string LoanProduct { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime DueDate { get; init; }
    public int DaysUntilDue { get; init; }
}

public sealed class PaymentHistoryItemDto
{
    public Guid PaymentId { get; init; }
    public string LoanProduct { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime PaidAtUtc { get; init; }
    public string Status { get; init; } = string.Empty; // OnTime, Late
}
```

---

## Query Handler — GetBorrowerApplicationsQueryHandler

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetBorrowerApplications;

public sealed class GetBorrowerApplicationsQueryHandler
    : IRequestHandler<GetBorrowerApplicationsQuery, IReadOnlyList<WizardApplicationSummaryDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IBorrowerRepository _borrowerRepo;

    public GetBorrowerApplicationsQueryHandler(
        ILoanApplicationRepository repository,
        ICurrentUserService currentUser,
        IBorrowerRepository borrowerRepo)
    {
        _repository = repository;
        _currentUser = currentUser;
        _borrowerRepo = borrowerRepo;
    }

    public async Task<IReadOnlyList<WizardApplicationSummaryDto>> Handle(
        GetBorrowerApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var borrower = await _borrowerRepo.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Borrower profile not found.");

        return await _repository
            .GetApplicationSummariesByBorrowerAsync(borrower.Id, cancellationToken);
    }
}
```

**Key pattern:** The handler resolves the current user's borrower profile, then queries applications scoped to that borrower. This ensures data isolation between borrowers.

---

## API Endpoints

```csharp
// WizardController.cs
[HttpGet("/api/borrower/applications")]
public async Task<ActionResult<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>>>
    GetBorrowerApplications(CancellationToken cancellationToken)
{
    var query = new GetBorrowerApplicationsQuery();
    var applications = await _sender.Send(query, cancellationToken);
    return Ok(ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>.Ok(applications));
}

// DashboardController.cs
[HttpGet("borrower/loans")]
[Authorize(Roles = "Borrower")]
public async Task<ActionResult<ApiResponse<IReadOnlyList<BorrowerLoanDto>>>>
    GetBorrowerLoans(CancellationToken cancellationToken)
{
    var result = await _sender.Send(new GetBorrowerLoansQuery(), cancellationToken);
    return Ok(ApiResponse<IReadOnlyList<BorrowerLoanDto>>.Ok(result));
}

[HttpGet("borrower/upcoming")]
[Authorize(Roles = "Borrower")]
public async Task<ActionResult<ApiResponse<BorrowerPaymentSummaryDto>>>
    GetBorrowerPaymentSummary(CancellationToken cancellationToken)
{
    var result = await _sender.Send(new GetBorrowerPaymentSummaryQuery(), cancellationToken);
    return Ok(ApiResponse<BorrowerPaymentSummaryDto>.Ok(result));
}
```

---

## Blazor Page — BorrowerDashboard.razor

```razor
@page "/borrower/dashboard"
@page "/borrower-dashboard"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Roles = "Borrower")]
@inject WizardApiClient WizardApi
@inject NavigationManager NavigationManager

<div class="max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-6">
        <div>
            <h1 class="text-2xl font-bold text-slate-900">Borrower Dashboard</h1>
            <p class="text-sm text-slate-500 mt-1">Track your applications, active loans, and payments.</p>
        </div>
        <a href="/wizard"
           class="rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-blue-700">
            + New Application
        </a>
    </div>

    <div class="flex gap-2 border-b border-slate-200 mb-6">
        <button class="px-4 py-2 text-sm font-semibold @TabClass("applications")"
                @onclick="() => _activeTab = \"applications\"">Applications</button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("loans")"
                @onclick="() => _activeTab = \"loans\"">Active Loans</button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("history")"
                @onclick="() => _activeTab = \"history\"">Payment History</button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("upcoming")"
                @onclick="() => _activeTab = \"upcoming\"">Upcoming Payments</button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("calculator")"
                @onclick="() => _activeTab = \"calculator\"">Calculator</button>
    </div>

    @switch (_activeTab)
    {
        case "loans":
            <BorrowerLoans />
            break;
        case "history":
            <BorrowerPaymentHistory />
            break;
        case "upcoming":
            <UpcomingPayments />
            break;
        case "calculator":
            <LoanCalculatorWidget />
            <div class="mt-6"><AffordabilityCheck /></div>
            break;
        default:
            @* Applications tab content rendered inline *@
            @RenderApplicationsList()
            break;
    }
</div>

@code {
    private IReadOnlyList<WizardApplicationSummaryDto>? _applications;
    private bool _isLoading = true;
    private string? _error;
    private string _activeTab = "applications";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await WizardApi.GetBorrowerApplicationsAsync();
            if (result is not null && result.Success)
                _applications = result.Data;
            else
                _error = result?.Errors.FirstOrDefault() ?? "Failed to load applications.";
        }
        catch (Exception ex)
        {
            _error = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string TabClass(string tab) =>
        _activeTab == tab
            ? "text-blue-600 border-b-2 border-blue-600"
            : "text-slate-500 hover:text-slate-700";

    private static string GetStatusLabel(int status) => status switch
    {
        0 => "Draft",
        1 => "Submitted",
        2 => "Under Review",
        3 => "Approved",
        4 => "Rejected",
        5 => "Funded",
        6 => "Closed",
        7 => "Withdrawn",
        8 => "Documents Requested",
        _ => "Unknown"
    };

    private static string GetStatusBadgeClass(int status) => status switch
    {
        0 => "bg-slate-100 text-slate-600",
        1 => "bg-blue-100 text-blue-700",
        2 => "bg-purple-100 text-purple-700",
        3 => "bg-green-100 text-green-700",
        4 => "bg-red-100 text-red-700",
        5 => "bg-emerald-100 text-emerald-700",
        7 => "bg-slate-100 text-slate-500",
        8 => "bg-amber-100 text-amber-700",
        _ => "bg-slate-100 text-slate-600"
    };
}
```

---

## Child Component — BorrowerLoans.razor

```razor
@inject DashboardApiClient DashboardApi

@if (_isLoading)
{
    <LoadingSkeleton Variant="cards" Columns="2" />
}
else if (_loans is not null && _loans.Count > 0)
{
    <div class="grid gap-4">
        @foreach (var loan in _loans)
        {
            <div class="rounded-xl bg-white border border-slate-200 p-5">
                <div class="flex justify-between items-start mb-3">
                    <div>
                        <h3 class="text-sm font-semibold text-slate-800">@loan.ProductName</h3>
                        <p class="text-xs text-slate-500">Lender: @loan.LenderName</p>
                    </div>
                    <StatusBadge Status="@loan.Status" />
                </div>

                <div class="grid grid-cols-3 gap-4 text-center mb-4">
                    <div>
                        <p class="text-xs text-slate-500">Original</p>
                        <p class="text-sm font-semibold">£@loan.OriginalAmount.ToString("N0")</p>
                    </div>
                    <div>
                        <p class="text-xs text-slate-500">Remaining</p>
                        <p class="text-sm font-semibold">£@loan.RemainingBalance.ToString("N0")</p>
                    </div>
                    <div>
                        <p class="text-xs text-slate-500">Monthly</p>
                        <p class="text-sm font-semibold">£@loan.MonthlyPayment.ToString("N2")</p>
                    </div>
                </div>

                @* Progress bar *@
                <div class="mb-2">
                    <div class="flex justify-between text-xs text-slate-500 mb-1">
                        <span>@loan.PaidInstallments of @loan.TotalInstallments payments</span>
                        <span>@ProgressPercent(loan)%</span>
                    </div>
                    <div class="h-2 rounded-full bg-slate-200 overflow-hidden">
                        <div class="h-full rounded-full bg-blue-500"
                             style="width: @ProgressPercent(loan)%"></div>
                    </div>
                </div>

                @if (loan.NextPaymentDate.HasValue)
                {
                    <p class="text-xs text-slate-500 mt-2">
                        Next payment: @loan.NextPaymentDate.Value.ToString("dd MMM yyyy")
                    </p>
                }
            </div>
        }
    </div>
}
else
{
    <EmptyState Icon="💳" Title="No active loans"
                Message="Once your application is funded, your loan will appear here." />
}

@code {
    private IReadOnlyList<BorrowerLoanDto>? _loans;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var response = await DashboardApi.GetBorrowerLoansAsync();
        _loans = response?.Data;
        _isLoading = false;
    }

    private static int ProgressPercent(BorrowerLoanDto loan) =>
        loan.TotalInstallments == 0 ? 0 :
        (int)Math.Round((double)loan.PaidInstallments / loan.TotalInstallments * 100);
}
```

---

## Child Component — UpcomingPayments.razor

```razor
@inject DashboardApiClient DashboardApi

@if (_isLoading)
{
    <LoadingSkeleton Variant="table" Rows="4" />
}
else if (_summary is not null)
{
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard Title="Total Paid" Value="@($"£{_summary.TotalPaidToDate:N0}")" Icon="✅" />
        <StatCard Title="Remaining" Value="@($"£{_summary.TotalRemaining:N0}")" Icon="📊" />
        <StatCard Title="Next Payment" Value="@($"£{_summary.NextPaymentAmount:N2}")" Icon="💳" />
        <StatCard Title="Due Date"
                  Value="@(_summary.NextPaymentDate?.ToString("dd MMM") ?? "—")" Icon="📅" />
    </div>

    @if (_summary.UpcomingPayments.Count > 0)
    {
        <div class="rounded-2xl bg-white border border-slate-200 overflow-hidden">
            <div class="px-5 py-3 border-b border-slate-100">
                <h3 class="text-sm font-semibold text-slate-700">Upcoming Payments</h3>
            </div>
            <div class="divide-y divide-slate-100">
                @foreach (var payment in _summary.UpcomingPayments)
                {
                    <div class="flex items-center justify-between px-5 py-3">
                        <div>
                            <p class="text-sm font-medium text-slate-800">@payment.LoanProduct</p>
                            <p class="text-xs text-slate-500">Due: @payment.DueDate.ToString("dd MMM yyyy")</p>
                        </div>
                        <div class="text-right">
                            <p class="text-sm font-semibold">£@payment.Amount.ToString("N2")</p>
                            <p class="text-xs @(payment.DaysUntilDue <= 3 ? "text-red-500" : "text-slate-500")">
                                @(payment.DaysUntilDue == 0 ? "Due today" :
                                  payment.DaysUntilDue == 1 ? "Due tomorrow" :
                                  $"In {payment.DaysUntilDue} days")
                            </p>
                        </div>
                    </div>
                }
            </div>
        </div>
    }
}

@code {
    private BorrowerPaymentSummaryDto? _summary;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var response = await DashboardApi.GetBorrowerPaymentSummaryAsync();
        _summary = response?.Data;
        _isLoading = false;
    }
}
```

---

## Child Component — LoanCalculatorWidget.razor

```razor
<div class="rounded-2xl bg-white border border-slate-200 p-6">
    <h3 class="text-lg font-semibold text-slate-800 mb-4">Loan Calculator</h3>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div>
            <label class="text-xs font-medium text-slate-600">Loan Amount (£)</label>
            <input type="number" @bind="_amount" @bind:event="oninput"
                   class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
            <label class="text-xs font-medium text-slate-600">Interest Rate (%)</label>
            <input type="number" step="0.1" @bind="_rate" @bind:event="oninput"
                   class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
        </div>
        <div>
            <label class="text-xs font-medium text-slate-600">Term (months)</label>
            <input type="number" @bind="_termMonths" @bind:event="oninput"
                   class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" />
        </div>
    </div>

    @if (MonthlyPayment > 0)
    {
        <div class="grid grid-cols-3 gap-4 text-center rounded-xl bg-slate-50 p-4">
            <div>
                <p class="text-xs text-slate-500">Monthly Payment</p>
                <p class="text-lg font-bold text-blue-600">£@MonthlyPayment.ToString("N2")</p>
            </div>
            <div>
                <p class="text-xs text-slate-500">Total Repayment</p>
                <p class="text-lg font-bold text-slate-800">£@TotalRepayment.ToString("N2")</p>
            </div>
            <div>
                <p class="text-xs text-slate-500">Total Interest</p>
                <p class="text-lg font-bold text-amber-600">£@TotalInterest.ToString("N2")</p>
            </div>
        </div>
    }
</div>

@code {
    private decimal _amount = 10000;
    private decimal _rate = 8.5m;
    private int _termMonths = 36;

    private decimal MonthlyPayment => CalculateMonthlyPayment();
    private decimal TotalRepayment => MonthlyPayment * _termMonths;
    private decimal TotalInterest => TotalRepayment - _amount;

    private decimal CalculateMonthlyPayment()
    {
        if (_amount <= 0 || _rate <= 0 || _termMonths <= 0) return 0;

        var monthlyRate = _rate / 100 / 12;
        var factor = (double)monthlyRate * Math.Pow(1 + (double)monthlyRate, _termMonths);
        var divisor = Math.Pow(1 + (double)monthlyRate, _termMonths) - 1;

        if (divisor == 0) return 0;

        return _amount * (decimal)(factor / divisor);
    }
}
```

---

## How the Layers Connect

```
BorrowerDashboard.razor
    ├── Applications tab (inline) → WizardApiClient.GetBorrowerApplicationsAsync()
    │       → GET /api/borrower/applications → GetBorrowerApplicationsQuery
    │
    ├── BorrowerLoans.razor → DashboardApiClient.GetBorrowerLoansAsync()
    │       → GET /api/dashboard/borrower/loans → GetBorrowerLoansQuery
    │
    ├── BorrowerPaymentHistory.razor → PaymentsApiClient.GetHistoryAsync()
    │       → GET /api/payments/history → GetPaymentHistoryQuery
    │
    ├── UpcomingPayments.razor → DashboardApiClient.GetBorrowerPaymentSummaryAsync()
    │       → GET /api/dashboard/borrower/upcoming → GetBorrowerPaymentSummaryQuery
    │
    └── LoanCalculatorWidget.razor (client-side only, no API call)
```

---

## Step-by-Step Extension Guide

### Adding a "Make Payment" button on upcoming payments

1. Add a button next to each upcoming payment
2. On click, navigate to `/payments?installmentId={id}` or open a payment modal
3. After payment, refresh the upcoming payments list

### Adding push notifications for due payments

1. Subscribe to `LoanHubClient.OnPaymentDue` event (requires new SignalR event)
2. Show a toast notification using `ToastService.ShowWarning()`
3. Refresh the upcoming payments component

---

## Testing Considerations

- **User scoping:** Verify that borrower A cannot see borrower B's applications.
- **Status mapping:** Test all 9 status values render correct labels and badge colors.
- **Calculator edge cases:** Zero amount, zero rate, zero term should all return 0.
- **Empty states:** Verify EmptyState component renders when no data exists.
