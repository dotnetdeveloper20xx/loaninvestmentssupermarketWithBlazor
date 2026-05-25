# 24 — Lender Dashboard

## Overview

The Lender Dashboard provides investors/lenders with a comprehensive view of their lending portfolio. It uses a tabbed interface with five sections: Portfolio (summary KPIs), Loans (individual funded loans), Earnings (interest income breakdown), Analytics (risk/return metrics), and Comparison (benchmarking against other lenders).

---

## Feature Requirements (Plain English)

1. Display portfolio summary: total invested, active loans count, average interest rate, total earnings, default rate.
2. List all funded loans with status (performing, late, defaulted), remaining balance, and next payment date.
3. Show earnings breakdown: total interest earned, monthly earnings trend, projected annual return.
4. Provide analytics: portfolio diversification by product type, risk distribution, ROI calculations.
5. Compare lender performance against platform averages.
6. Tab navigation between sections without page reload.
7. Only accessible to `Lender` and `Admin` roles.

---

## Technologies & Patterns

| Layer | Technology | Pattern |
|-------|-----------|---------|
| Application | MediatR queries | CQRS read-side |
| Infrastructure | EF Core | Repository + projections |
| API | ASP.NET Controller | Multiple endpoints under `/api/dashboard/lender/` |
| Frontend | Blazor WASM | Tab-based component composition |

---

## Application Layer Queries

### GetLenderDashboardQuery (Portfolio)

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetLenderDashboard/GetLenderDashboardQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;

public sealed record GetLenderDashboardQuery : IRequest<LenderPortfolioDto>;
```

### GetLenderLoansQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetLenderLoans/GetLenderLoansQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderLoans;

public sealed record GetLenderLoansQuery(
    string? Performance = null,
    string? SortBy = null
) : IRequest<IReadOnlyList<LenderLoanDto>>;
```

### GetLenderEarningsQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetLenderEarnings/GetLenderEarningsQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderEarnings;

public sealed record GetLenderEarningsQuery : IRequest<LenderEarningsDto>;
```

### GetInvestorAnalyticsQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetInvestorAnalytics/GetInvestorAnalyticsQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetInvestorAnalytics;

public sealed record GetInvestorAnalyticsQuery : IRequest<InvestorAnalyticsDto>;
```

---

## Shared DTOs

```csharp
// src/LoanSuperMarket.Shared/Dashboard/LenderPortfolioDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderPortfolioDto
{
    public decimal TotalInvested { get; init; }
    public int ActiveLoansCount { get; init; }
    public decimal AverageInterestRate { get; init; }
    public decimal TotalEarnings { get; init; }
    public decimal DefaultRate { get; init; }
    public decimal AvailableCapital { get; init; }
    public List<PortfolioAllocationDto> Allocations { get; init; } = [];
}

public sealed class PortfolioAllocationDto
{
    public string ProductName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
}
```

```csharp
// src/LoanSuperMarket.Shared/Dashboard/LenderLoanDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderLoanDto
{
    public Guid LoanId { get; init; }
    public string BorrowerName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal OriginalAmount { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal InterestRate { get; init; }
    public string Performance { get; init; } = string.Empty; // Performing, Late, Defaulted
    public DateTime? NextPaymentDate { get; init; }
    public DateTime FundedAtUtc { get; init; }
}
```

```csharp
// src/LoanSuperMarket.Shared/Dashboard/LenderEarningsDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderEarningsDto
{
    public decimal TotalInterestEarned { get; init; }
    public decimal ThisMonthEarnings { get; init; }
    public decimal ProjectedAnnualReturn { get; init; }
    public List<MonthlyEarningDto> MonthlyTrend { get; init; } = [];
}

public sealed class MonthlyEarningDto
{
    public string Month { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
```

```csharp
// src/LoanSuperMarket.Shared/Dashboard/InvestorAnalyticsDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class InvestorAnalyticsDto
{
    public decimal WeightedAverageReturn { get; init; }
    public decimal PortfolioRiskScore { get; init; }
    public int DiversificationScore { get; init; }
    public List<RiskBucketDto> RiskDistribution { get; init; } = [];
    public decimal PlatformAverageReturn { get; init; }
    public decimal YourReturnVsPlatform { get; init; }
}

public sealed class RiskBucketDto
{
    public string Label { get; init; } = string.Empty; // Low, Medium, High
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}
```

---

## Query Handler Example — GetLenderDashboardQueryHandler

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetLenderDashboard/GetLenderDashboardQueryHandler.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;

public sealed class GetLenderDashboardQueryHandler
    : IRequestHandler<GetLenderDashboardQuery, LenderPortfolioDto>
{
    private readonly ILenderRepository _lenderRepo;
    private readonly ILoanApplicationRepository _loanRepo;
    private readonly ICurrentUserService _currentUser;

    public GetLenderDashboardQueryHandler(
        ILenderRepository lenderRepo,
        ILoanApplicationRepository loanRepo,
        ICurrentUserService currentUser)
    {
        _lenderRepo = lenderRepo;
        _loanRepo = loanRepo;
        _currentUser = currentUser;
    }

    public async Task<LenderPortfolioDto> Handle(
        GetLenderDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var lender = await _lenderRepo.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Lender profile not found.");

        // Query funded loans for this lender
        var fundedLoans = await _loanRepo
            .GetFundedByLenderAsync(lender.Id, cancellationToken);

        var totalInvested = fundedLoans.Sum(l => l.RequestedAmount.Amount);
        var activeCount = fundedLoans.Count(l => l.IsActive);
        var avgRate = fundedLoans.Any()
            ? fundedLoans.Average(l => l.InterestRate)
            : 0;

        return new LenderPortfolioDto
        {
            TotalInvested = totalInvested,
            ActiveLoansCount = activeCount,
            AverageInterestRate = Math.Round(avgRate, 2),
            TotalEarnings = fundedLoans.Sum(l => l.TotalInterestPaid),
            DefaultRate = CalculateDefaultRate(fundedLoans),
            AvailableCapital = lender.AvailableCapital.Amount
        };
    }

    private static decimal CalculateDefaultRate(IReadOnlyList<FundedLoan> loans)
    {
        if (loans.Count == 0) return 0;
        var defaulted = loans.Count(l => l.IsDefaulted);
        return Math.Round((decimal)defaulted / loans.Count * 100, 2);
    }
}
```

---

## API Endpoints

```csharp
// DashboardController.cs (lender endpoints)
[HttpGet("lender/portfolio")]
[Authorize(Roles = "Lender,Admin")]
public async Task<ActionResult<ApiResponse<LenderPortfolioDto>>> GetLenderPortfolio(
    CancellationToken cancellationToken)
{
    var result = await _sender.Send(new GetLenderDashboardQuery(), cancellationToken);
    return Ok(ApiResponse<LenderPortfolioDto>.Ok(result));
}

[HttpGet("lender/loans")]
[Authorize(Roles = "Lender,Admin")]
public async Task<ActionResult<ApiResponse<IReadOnlyList<LenderLoanDto>>>> GetLenderLoans(
    [FromQuery] string? performance,
    [FromQuery] string? sortBy,
    CancellationToken cancellationToken)
{
    var result = await _sender.Send(
        new GetLenderLoansQuery(performance, sortBy), cancellationToken);
    return Ok(ApiResponse<IReadOnlyList<LenderLoanDto>>.Ok(result));
}

[HttpGet("lender/earnings")]
[Authorize(Roles = "Lender,Admin")]
public async Task<ActionResult<ApiResponse<LenderEarningsDto>>> GetLenderEarnings(
    CancellationToken cancellationToken)
{
    var result = await _sender.Send(new GetLenderEarningsQuery(), cancellationToken);
    return Ok(ApiResponse<LenderEarningsDto>.Ok(result));
}

[HttpGet("lender/analytics")]
[Authorize(Roles = "Lender,Admin")]
public async Task<ActionResult<ApiResponse<InvestorAnalyticsDto>>> GetInvestorAnalytics(
    CancellationToken cancellationToken)
{
    var result = await _sender.Send(new GetInvestorAnalyticsQuery(), cancellationToken);
    return Ok(ApiResponse<InvestorAnalyticsDto>.Ok(result));
}
```

---

## Blazor API Client Methods

```csharp
// DashboardApiClient.cs (lender methods)
public async Task<ApiResponse<LenderPortfolioDto>?> GetLenderPortfolioAsync(
    CancellationToken cancellationToken = default)
{
    return await _httpClient.GetFromJsonAsync<ApiResponse<LenderPortfolioDto>>(
        "api/dashboard/lender/portfolio", cancellationToken);
}

public async Task<ApiResponse<IReadOnlyList<LenderLoanDto>>?> GetLenderLoansAsync(
    string? performance = null,
    string? sortBy = null,
    CancellationToken cancellationToken = default)
{
    var queryParams = new List<string>();
    if (!string.IsNullOrWhiteSpace(performance))
        queryParams.Add($"performance={Uri.EscapeDataString(performance)}");
    if (!string.IsNullOrWhiteSpace(sortBy))
        queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

    var url = "api/dashboard/lender/loans";
    if (queryParams.Count > 0)
        url += "?" + string.Join("&", queryParams);

    return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<LenderLoanDto>>>(
        url, cancellationToken);
}

public async Task<ApiResponse<LenderEarningsDto>?> GetLenderEarningsAsync(
    CancellationToken cancellationToken = default)
{
    return await _httpClient.GetFromJsonAsync<ApiResponse<LenderEarningsDto>>(
        "api/dashboard/lender/earnings", cancellationToken);
}

public async Task<ApiResponse<InvestorAnalyticsDto>?> GetInvestorAnalyticsAsync(
    CancellationToken cancellationToken = default)
{
    return await _httpClient.GetFromJsonAsync<ApiResponse<InvestorAnalyticsDto>>(
        "api/dashboard/lender/analytics", cancellationToken);
}
```

---

## Blazor Page — LenderDashboard.razor

```razor
@page "/lender-dashboard"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Roles = "Lender,Admin")]

<PageHeader Title="Lender Dashboard"
            Subtitle="Monitor your lending portfolio, funded loans, and earnings." />

<div class="mb-6">
    <div class="flex gap-2 border-b border-slate-200 mb-6">
        <button class="px-4 py-2 text-sm font-semibold @TabClass("portfolio")"
                @onclick="() => _activeTab = \"portfolio\"">
            Portfolio
        </button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("loans")"
                @onclick="() => _activeTab = \"loans\"">
            Loans
        </button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("earnings")"
                @onclick="() => _activeTab = \"earnings\"">
            Earnings
        </button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("analytics")"
                @onclick="() => _activeTab = \"analytics\"">
            Analytics
        </button>
        <button class="px-4 py-2 text-sm font-semibold @TabClass("comparison")"
                @onclick="() => _activeTab = \"comparison\"">
            Comparison
        </button>
    </div>

    @switch (_activeTab)
    {
        case "portfolio":
            <LenderPortfolio />
            break;
        case "loans":
            <LenderLoans />
            break;
        case "earnings":
            <LenderEarnings />
            break;
        case "analytics":
            <InvestorAnalytics />
            break;
        case "comparison":
            <LenderProductComparison />
            break;
    }
</div>

@code {
    private string _activeTab = "portfolio";

    private string TabClass(string tab) =>
        _activeTab == tab
            ? "text-blue-600 border-b-2 border-blue-600"
            : "text-slate-500 hover:text-slate-700";
}
```

---

## Child Component — LenderPortfolio.razor

```razor
@inject DashboardApiClient DashboardApi

@if (_isLoading)
{
    <LoadingSkeleton Variant="cards" Columns="4" />
}
else if (_portfolio is not null)
{
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard Title="Total Invested" Value="@FormatCurrency(_portfolio.TotalInvested)" Icon="💰" />
        <StatCard Title="Active Loans" Value="@_portfolio.ActiveLoansCount.ToString()" Icon="📊" />
        <StatCard Title="Avg Interest" Value="@($"{_portfolio.AverageInterestRate:N1}%")" Icon="📈" />
        <StatCard Title="Total Earnings" Value="@FormatCurrency(_portfolio.TotalEarnings)" Icon="💷" />
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div class="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 class="text-sm font-semibold text-slate-700 mb-3">Available Capital</h3>
            <p class="text-2xl font-bold text-emerald-600">£@_portfolio.AvailableCapital.ToString("N0")</p>
        </div>
        <div class="rounded-2xl bg-white border border-slate-200 p-5">
            <h3 class="text-sm font-semibold text-slate-700 mb-3">Default Rate</h3>
            <p class="text-2xl font-bold @(_portfolio.DefaultRate > 5 ? "text-red-600" : "text-slate-900")">
                @_portfolio.DefaultRate.ToString("N1")%
            </p>
        </div>
    </div>
}
else if (_error is not null)
{
    <div class="text-red-600 text-sm">@_error</div>
}

@code {
    private LenderPortfolioDto? _portfolio;
    private bool _isLoading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await DashboardApi.GetLenderPortfolioAsync();
            if (response?.Success == true)
                _portfolio = response.Data;
            else
                _error = response?.Errors.FirstOrDefault() ?? "Failed to load portfolio.";
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatCurrency(decimal value) =>
        value >= 1_000_000 ? $"£{value / 1_000_000:N1}M" :
        value >= 1_000 ? $"£{value / 1_000:N0}K" :
        $"£{value:N0}";
}
```

---

## Child Component — LenderLoans.razor

```razor
@inject DashboardApiClient DashboardApi

@if (_isLoading)
{
    <LoadingSkeleton Variant="table" Rows="5" />
}
else if (_loans is not null && _loans.Count > 0)
{
    <div class="flex gap-2 mb-4">
        <button class="px-3 py-1.5 text-xs rounded-lg @FilterClass(null)"
                @onclick="() => SetFilter(null)">All</button>
        <button class="px-3 py-1.5 text-xs rounded-lg @FilterClass("Performing")"
                @onclick="() => SetFilter(\"Performing\")">Performing</button>
        <button class="px-3 py-1.5 text-xs rounded-lg @FilterClass("Late")"
                @onclick="() => SetFilter(\"Late\")">Late</button>
        <button class="px-3 py-1.5 text-xs rounded-lg @FilterClass("Defaulted")"
                @onclick="() => SetFilter(\"Defaulted\")">Defaulted</button>
    </div>

    <div class="rounded-2xl bg-white border border-slate-200 overflow-hidden">
        <table class="w-full text-sm">
            <thead class="bg-slate-50 text-left">
                <tr>
                    <th class="px-4 py-3 font-medium text-slate-600">Borrower</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Product</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Amount</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Balance</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Rate</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Status</th>
                    <th class="px-4 py-3 font-medium text-slate-600">Next Payment</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
                @foreach (var loan in _loans)
                {
                    <tr class="hover:bg-slate-50">
                        <td class="px-4 py-3">@loan.BorrowerName</td>
                        <td class="px-4 py-3">@loan.ProductName</td>
                        <td class="px-4 py-3">£@loan.OriginalAmount.ToString("N0")</td>
                        <td class="px-4 py-3">£@loan.RemainingBalance.ToString("N0")</td>
                        <td class="px-4 py-3">@loan.InterestRate%</td>
                        <td class="px-4 py-3">
                            <StatusBadge Status="@loan.Performance" />
                        </td>
                        <td class="px-4 py-3">
                            @(loan.NextPaymentDate?.ToString("dd MMM") ?? "—")
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}
else
{
    <EmptyState Icon="📊" Title="No funded loans" Message="Fund a loan to see it here." />
}

@code {
    private IReadOnlyList<LenderLoanDto>? _loans;
    private bool _isLoading = true;
    private string? _performanceFilter;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        var response = await DashboardApi.GetLenderLoansAsync(_performanceFilter);
        _loans = response?.Data;
        _isLoading = false;
    }

    private async Task SetFilter(string? filter)
    {
        _performanceFilter = filter;
        await LoadAsync();
    }

    private string FilterClass(string? filter) =>
        _performanceFilter == filter
            ? "bg-blue-100 text-blue-700 font-medium"
            : "bg-slate-100 text-slate-600";
}
```

---

## How the Layers Connect

```
LenderDashboard.razor (page with tabs)
    ├── LenderPortfolio.razor → DashboardApiClient.GetLenderPortfolioAsync()
    │       → GET /api/dashboard/lender/portfolio
    │       → GetLenderDashboardQuery → Handler → Repository
    │
    ├── LenderLoans.razor → DashboardApiClient.GetLenderLoansAsync(filter)
    │       → GET /api/dashboard/lender/loans?performance=Late
    │       → GetLenderLoansQuery → Handler → Repository
    │
    ├── LenderEarnings.razor → DashboardApiClient.GetLenderEarningsAsync()
    │       → GET /api/dashboard/lender/earnings
    │       → GetLenderEarningsQuery → Handler → Repository
    │
    └── InvestorAnalytics.razor → DashboardApiClient.GetInvestorAnalyticsAsync()
            → GET /api/dashboard/lender/analytics
            → GetInvestorAnalyticsQuery → Handler → Repository
```

---

## Step-by-Step Extension Guide

### Adding a "Loan Details" drill-down from the Loans tab

1. Add a click handler on the table row:
   ```razor
   <tr @onclick="() => NavigateToLoan(loan.LoanId)" class="cursor-pointer hover:bg-slate-50">
   ```

2. Inject `NavigationManager` and navigate:
   ```csharp
   @inject NavigationManager Nav
   private void NavigateToLoan(Guid id) => Nav.NavigateTo($"/loan/{id}");
   ```

### Adding real-time updates when a payment is received

1. Inject `LoanHubClient` in `LenderPortfolio.razor`
2. Subscribe to `OnPaymentRecorded`:
   ```csharp
   protected override void OnInitialized()
   {
       LoanHubClient.OnPaymentRecorded += HandlePaymentRecorded;
   }

   private async void HandlePaymentRecorded(Guid scheduleId, decimal amount)
   {
       await InvokeAsync(async () =>
       {
           await LoadAsync();
           StateHasChanged();
       });
   }
   ```

### Adding a new tab (e.g., "Tax Reports")

1. Add button in the tab bar
2. Create `LenderTaxReports.razor` component
3. Add case in the `@switch` block
4. Create corresponding query/handler/endpoint

---

## Testing Considerations

- **Handler tests:** Mock `ILenderRepository` and `ICurrentUserService`, verify correct user scoping.
- **API tests:** Verify 401 for unauthenticated, 403 for non-Lender roles.
- **Component tests:** Use bUnit, mock `DashboardApiClient`, verify tab switching renders correct child.
