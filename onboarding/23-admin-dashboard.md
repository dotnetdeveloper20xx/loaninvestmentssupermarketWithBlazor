# 23 — Admin Dashboard

## Overview

The Admin Dashboard is the first screen administrators see after login. It provides a high-level summary of platform health: total funding volume, application counts, borrower/lender counts, conversion metrics (approval rate, funding rate), recent applications, quick-action links, and newly registered borrowers.

This document covers the full vertical slice from the database query through the API layer to the Blazor WASM component.

---

## Feature Requirements (Plain English)

1. Display KPI cards: Total Funded (£), Total Applications, Total Borrowers, Total Lenders, Total Products.
2. Show conversion metrics: Approval Rate (%), Funding Rate (%), Funded Loans count.
3. List the 5 most recent loan applications with status badges.
4. Provide quick-action links to Review Queue, Funding Queue, All Loans, Collections, User Management.
5. Show the 4 most recently registered borrowers with initials and join date.
6. Handle loading state with skeleton UI and error state with a red alert box.
7. Only accessible to users with the `Admin` role.

---

## Technologies & Patterns

| Layer | Technology | Pattern |
|-------|-----------|---------|
| Domain | C# record DTOs | Shared DTOs in `LoanSuperMarket.Shared` |
| Application | MediatR query/handler | CQRS (query side) |
| Infrastructure | EF Core + LINQ | Repository pattern |
| API | ASP.NET Core Controller | Thin controller, delegates to MediatR |
| Frontend | Blazor WASM component | Injected API client, conditional rendering |

---

## Layer 1: Shared DTOs

The DTOs live in `LoanSuperMarket.Shared.Dashboard` so both the API and Blazor projects can reference them without circular dependencies.

```csharp
// src/LoanSuperMarket.Shared/Dashboard/DashboardSummaryDto.cs
namespace LoanSuperMarket.Shared.Dashboard;

public sealed class DashboardSummaryDto
{
    public int TotalBorrowers { get; init; }
    public int TotalLenders { get; init; }
    public int TotalLoanProducts { get; init; }
    public int TotalApplications { get; init; }
    public int ApprovedApplications { get; init; }
    public int FundedApplications { get; init; }
    public decimal TotalFundingVolume { get; init; }
    public decimal ApprovalRate { get; init; }
    public decimal FundingRate { get; init; }
    public List<RecentApplicationDto> RecentApplications { get; init; } = [];
    public List<RecentBorrowerDto> RecentBorrowers { get; init; } = [];
}

public sealed class RecentApplicationDto
{
    public Guid Id { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
}

public sealed class RecentBorrowerDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
```

**Key design decision:** DTOs are in the Shared project because both the API (serializes them) and Blazor (deserializes them) need the same shape. This avoids duplicating models.

---

## Layer 2: Application Layer — Query & Handler

### GetDashboardSummaryQuery

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetDashboardSummaryQuery.cs
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard;

public sealed record GetDashboardSummaryQuery
    : IRequest<DashboardSummaryDto>;
```

This is a parameter-less query — the dashboard always shows the full platform summary.

### GetDashboardSummaryQueryHandler

```csharp
// src/LoanSuperMarket.Application/Features/Dashboard/GetDashboardSummaryQueryHandler.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard;

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDashboardRepository _repository;

    public GetDashboardSummaryQueryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetDashboardSummaryAsync(cancellationToken);
    }
}
```

**Pattern:** The handler is intentionally thin — it delegates to the repository. Business logic (like rate calculations) lives in the repository because it's purely a read-model concern.

### IDashboardRepository Interface

```csharp
// src/LoanSuperMarket.Application/Common/Interfaces/IDashboardRepository.cs
using LoanSuperMarket.Shared.Dashboard;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken);
}
```

---

## Layer 3: Infrastructure — DashboardRepository

```csharp
// src/LoanSuperMarket.Infrastructure/Repositories/DashboardRepository.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken)
    {
        var totalBorrowers = await _context.Borrowers.CountAsync(cancellationToken);
        var totalLenders = await _context.Lenders.CountAsync(cancellationToken);
        var totalLoanProducts = await _context.LoanProducts.CountAsync(cancellationToken);
        var totalApplications = await _context.LoanApplications.CountAsync(cancellationToken);

        var approvedApplications = await _context.LoanApplications
            .CountAsync(
                x => x.Status == LoanApplicationStatus.Approved,
                cancellationToken);

        var fundedApplications = await _context.LoanApplications
            .CountAsync(
                x => x.Status == LoanApplicationStatus.Funded,
                cancellationToken);

        var totalFundingVolume = await _context.LoanApplications
            .Where(x => x.Status == LoanApplicationStatus.Funded)
            .SumAsync(
                x => (decimal?)x.RequestedAmount.Amount,
                cancellationToken) ?? 0;

        var approvalRate = totalApplications == 0
            ? 0
            : Math.Round((decimal)approvedApplications / totalApplications * 100, 2);

        var fundingRate = totalApplications == 0
            ? 0
            : Math.Round((decimal)fundedApplications / totalApplications * 100, 2);

        var recentApplications = await _context.LoanApplications
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Take(5)
            .Select(x => new RecentApplicationDto
            {
                Id = x.Id,
                Purpose = x.Purpose,
                Amount = x.RequestedAmount.Amount,
                Status = x.Status.ToString(),
                SubmittedAtUtc = x.SubmittedAtUtc ?? DateTime.MinValue
            })
            .ToListAsync(cancellationToken);

        var recentBorrowers = await _context.Borrowers
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new RecentBorrowerDto
            {
                Id = x.Id,
                FullName = x.FirstName + " " + x.LastName,
                Email = x.Email,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalBorrowers = totalBorrowers,
            TotalLenders = totalLenders,
            TotalLoanProducts = totalLoanProducts,
            TotalApplications = totalApplications,
            ApprovedApplications = approvedApplications,
            FundedApplications = fundedApplications,
            TotalFundingVolume = totalFundingVolume,
            ApprovalRate = approvalRate,
            FundingRate = fundingRate,
            RecentApplications = recentApplications,
            RecentBorrowers = recentBorrowers
        };
    }
}
```

**Key points:**
- Uses `(decimal?)` cast with null-coalescing to handle empty `SumAsync` results.
- `RequestedAmount.Amount` accesses a value object's property — EF Core maps this via owned entity configuration.
- Rate calculations use integer division guard (`totalApplications == 0`).

---

## Layer 4: API Controller

```csharp
// src/LoanSuperMarket.Api/Controllers/DashboardController.cs
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,CrmManager")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        CancellationToken cancellationToken)
    {
        var summary = await _sender.Send(
            new GetDashboardSummaryQuery(), cancellationToken);

        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}
```

The controller is a thin pass-through. It wraps the result in `ApiResponse<T>` for consistent JSON structure.

---

## Layer 5: Blazor API Client

```csharp
// src/LoanSuperMarket.Blazor/Services/ApiClients/DashboardApiClient.cs
using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class DashboardApiClient
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<DashboardSummaryDto>?> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<DashboardSummaryDto>>(
            "api/dashboard/summary",
            cancellationToken);
    }
}
```

Registered in `Program.cs`:
```csharp
builder.Services.AddScoped<DashboardApiClient>();
```

---

## Layer 6: Blazor Component — AdminDashboardView.razor

```razor
@using LoanSuperMarket.Shared.Dashboard
@inject DashboardApiClient DashboardApiClient
@inject NavigationManager Navigation

@if (_isLoading)
{
    <LoadingSkeleton Variant="dashboard" Rows="3" />
}
else if (_summary is not null)
{
    @* ─── KPI Strip ─── *@
    <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-8">
        <div class="rounded-2xl bg-white border border-slate-200 p-5 shadow-sm">
            <div class="flex items-center justify-between">
                <div>
                    <p class="text-xs font-semibold text-slate-500 uppercase tracking-wide">Total Funded</p>
                    <p class="mt-2 text-2xl font-bold text-slate-900">£@FormatCompact(_summary.TotalFundingVolume)</p>
                </div>
                <div class="h-10 w-10 rounded-xl bg-emerald-100 flex items-center justify-center text-lg">💷</div>
            </div>
        </div>
        <!-- Additional KPI cards for Applications, Borrowers, Lenders, Products -->
    </div>

    @* ─── Conversion Metrics ─── *@
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <div class="rounded-2xl bg-slate-900 p-6 text-white shadow-lg">
            <p class="text-sm font-medium text-slate-300">Approval Rate</p>
            <p class="mt-1 text-3xl font-bold">@_summary.ApprovalRate.ToString("N1")%</p>
            <div class="mt-3 h-2 rounded-full bg-slate-700 overflow-hidden">
                <div class="h-full rounded-full bg-blue-400"
                     style="width: @_summary.ApprovalRate%"></div>
            </div>
        </div>
        <!-- Funding Rate and Funded Loans cards -->
    </div>

    @* ─── Recent Applications + Quick Actions ─── *@
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 rounded-2xl bg-white border border-slate-200 shadow-sm">
            <!-- Recent applications list with click navigation -->
        </div>
        <div class="space-y-4">
            <!-- Quick Actions panel -->
            <!-- New Borrowers panel -->
        </div>
    </div>
}
else if (!string.IsNullOrWhiteSpace(_error))
{
    <div class="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700">
        @_error
    </div>
}

@code {
    private DashboardSummaryDto? _summary;
    private bool _isLoading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await DashboardApiClient.GetSummaryAsync();
            if (response?.Success == true)
            {
                _summary = response.Data;
            }
            else
            {
                _error = response?.Errors.FirstOrDefault() ?? "Failed to load dashboard.";
            }
        }
        catch (Exception ex)
        {
            _error = $"Unable to load dashboard. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatCompact(decimal value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000:N1}M";
        if (value >= 1_000) return $"{value / 1_000:N0}K";
        return value.ToString("N0");
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        return name.Length >= 2 ? name[..2].ToUpper() : "?";
    }

    private static string GetStatusClass(string status) => status.ToLowerInvariant() switch
    {
        "approved" => "bg-green-100 text-green-700",
        "funded" => "bg-emerald-100 text-emerald-700",
        "submitted" => "bg-blue-100 text-blue-700",
        "under review" or "underreview" => "bg-purple-100 text-purple-700",
        "rejected" => "bg-red-100 text-red-700",
        "draft" => "bg-slate-100 text-slate-600",
        _ => "bg-slate-100 text-slate-600"
    };

    private void NavigateToApplication(Guid id)
    {
        Navigation.NavigateTo($"/review-queue/{id}");
    }
}
```

---

## How the Layers Connect

```
Browser (Admin)
    │
    ▼
AdminDashboardView.razor
    │  calls DashboardApiClient.GetSummaryAsync()
    ▼
HttpClient → GET /api/dashboard/summary (with Bearer token via AuthTokenHandler)
    │
    ▼
DashboardController.GetSummary()
    │  sends GetDashboardSummaryQuery via MediatR
    ▼
GetDashboardSummaryQueryHandler
    │  calls IDashboardRepository.GetDashboardSummaryAsync()
    ▼
DashboardRepository (EF Core queries against SQL Server)
    │
    ▼
Returns DashboardSummaryDto → serialized as ApiResponse<DashboardSummaryDto>
    │
    ▼
Blazor deserializes → renders KPI cards, metrics, lists
```

---

## DI Registration

```csharp
// Infrastructure DependencyInjection.cs
services.AddScoped<IDashboardRepository, DashboardRepository>();

// Blazor Program.cs
builder.Services.AddScoped<DashboardApiClient>();
```

---

## Step-by-Step Extension Guide

### Adding a new KPI (e.g., "Total Revenue This Month")

1. **Add property to DTO:**
   ```csharp
   // DashboardSummaryDto.cs
   public decimal RevenueThisMonth { get; init; }
   ```

2. **Compute in repository:**
   ```csharp
   // DashboardRepository.cs
   var revenueThisMonth = await _context.Payments
       .Where(p => p.PaidAtUtc.Month == DateTime.UtcNow.Month)
       .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;
   ```

3. **Add card in Razor:**
   ```razor
   <div class="rounded-2xl bg-white border border-slate-200 p-5 shadow-sm">
       <p class="text-xs font-semibold text-slate-500 uppercase">Revenue (Month)</p>
       <p class="mt-2 text-2xl font-bold text-slate-900">£@FormatCompact(_summary.RevenueThisMonth)</p>
   </div>
   ```

No changes needed in the query, handler, controller, or API client — the DTO flows through unchanged.

### Adding a new section (e.g., "Overdue Loans Alert")

1. Add a new DTO: `OverdueLoanAlertDto`
2. Add property to `DashboardSummaryDto`: `List<OverdueLoanAlertDto> OverdueAlerts`
3. Query in `DashboardRepository`
4. Render in `AdminDashboardView.razor` with a red-themed card

### Making the dashboard cacheable

Implement `ICacheableQuery` on the query:

```csharp
public sealed record GetDashboardSummaryQuery
    : IRequest<DashboardSummaryDto>, ICacheableQuery
{
    public string CacheKey => "dashboard-summary";
    public int CacheMinutes => 2;
}
```

The `CachingBehaviour` pipeline will automatically cache the result for 2 minutes.

---

## Testing Considerations

- **Unit test the handler:** Mock `IDashboardRepository`, verify it's called once.
- **Integration test the repository:** Use an in-memory database, seed data, verify counts.
- **Component test:** Use bUnit to render `AdminDashboardView`, mock `DashboardApiClient`, verify KPI cards render.

---

## Common Pitfalls

1. **Null reference on `_summary.RecentApplications`** — Always initialize lists in DTOs with `= []`.
2. **Division by zero** — Guard rate calculations with `totalApplications == 0`.
3. **Slow queries** — Consider caching (see `ICacheableQuery`) or a materialized view for large datasets.
4. **Auth mismatch** — The controller requires `Admin,CrmManager` roles; ensure the Blazor page also has `[Authorize(Roles = "Admin")]`.
