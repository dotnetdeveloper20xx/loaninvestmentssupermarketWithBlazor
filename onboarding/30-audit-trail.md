# 30 — Audit Trail

## Overview

The audit trail records significant actions performed on the platform: loan approvals, funding decisions, status changes, user management actions, and more. Each audit entry captures what happened, to which entity, who did it, and when. This provides a complete history for compliance, debugging, and accountability.

---

## Feature Requirements (Plain English)

1. Record audit entries for all significant business actions (approve, reject, fund, create, update, delete).
2. Each entry captures: entity name, entity ID, action, description, performer, timestamp.
3. Audit logs are immutable — once created, they cannot be modified or deleted.
4. Query audit logs by entity (show history of a specific loan application).
5. Query recent audit logs (admin dashboard, audit log page).
6. Display audit trail in the UI with timeline visualization.

---

## Technologies & Patterns

| Layer | Technology | Pattern |
|-------|-----------|---------|
| Domain | AuditLog entity with factory | Rich domain model |
| Application | IAuditLogRepository | Repository pattern |
| Infrastructure | EF Core | Append-only persistence |
| API | Controller endpoint | REST query |
| Frontend | Blazor component | Timeline UI |

---

## Domain Entity: AuditLog

```csharp
// src/LoanSuperMarket.Domain/Entities/AuditLog.cs
using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    private AuditLog()
    {
        EntityName = string.Empty;
        Action = string.Empty;
        Description = string.Empty;
        PerformedBy = string.Empty;
    }

    private AuditLog(
        string entityName,
        Guid? entityId,
        string action,
        string description,
        string performedBy)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Description = description;
        PerformedBy = performedBy;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public string EntityName { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Action { get; private set; }
    public string Description { get; private set; }
    public string PerformedBy { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Factory method with validation. The only way to create an AuditLog.
    /// </summary>
    public static AuditLog Create(
        string entityName,
        Guid? entityId,
        string action,
        string description,
        string performedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new DomainException("Audit entity name is required.");

        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("Audit action is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Audit description is required.");

        return new AuditLog(
            entityName.Trim(),
            entityId,
            action.Trim(),
            description.Trim(),
            string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy.Trim());
    }
}
```

### Design Decisions

1. **Private constructor** — Forces use of the `Create()` factory method, ensuring validation always runs.
2. **Private setters** — Makes the entity immutable after creation.
3. **`performedBy` defaults to "System"** — For background service actions where there's no user context.
4. **`EntityId` is nullable** — Some actions (like "system started") don't relate to a specific entity.

---

## Application Layer: IAuditLogRepository

```csharp
// src/LoanSuperMarket.Application/Common/Interfaces/IAuditLogRepository.cs
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Audit;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName, Guid entityId, CancellationToken cancellationToken);
}
```

---

## Infrastructure: AuditLogRepository

```csharp
// src/LoanSuperMarket.Infrastructure/Repositories/AuditLogRepository.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Audit;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        int take, CancellationToken cancellationToken)
    {
        take = take is < 1 or > 100 ? 20 : take;

        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                Description = x.Description,
                PerformedBy = x.PerformedBy,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName, Guid entityId, CancellationToken cancellationToken)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                Description = x.Description,
                PerformedBy = x.PerformedBy,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
```

---

## Recording Audit Entries in Command Handlers

### Pattern: Audit at the end of the command handler

```csharp
// Example: ApproveLoanApplicationCommandHandler
public sealed class ApproveLoanApplicationCommandHandler
    : IRequestHandler<ApproveLoanApplicationCommand, Unit>
{
    private readonly ILoanApplicationRepository _loanRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ICurrentUserService _currentUser;

    public async Task<Unit> Handle(
        ApproveLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _loanRepo.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Application not found.");

        // Business logic
        application.Approve();
        await _loanRepo.SaveChangesAsync(cancellationToken);

        // Record audit entry
        var auditLog = AuditLog.Create(
            entityName: "LoanApplication",
            entityId: application.Id,
            action: "Approved",
            description: $"Loan application for £{application.RequestedAmount.Amount:N0} approved.",
            performedBy: _currentUser.Email ?? "Unknown");

        await _auditRepo.AddAsync(auditLog, cancellationToken);
        await _auditRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

### Pattern: Audit in the FundLoan handler

```csharp
var auditLog = AuditLog.Create(
    entityName: "LoanApplication",
    entityId: application.Id,
    action: "Funded",
    description: $"Loan funded by {lender.CompanyName}. Amount: £{amount:N0}. " +
                 $"Rate: {product.InterestRate}%. Term: {application.TermMonths} months.",
    performedBy: _currentUser.Email ?? "System");
```

---

## When to Create Audit Logs

| Action | Entity | Example Description |
|--------|--------|-------------------|
| Application submitted | LoanApplication | "Application submitted for review" |
| Application approved | LoanApplication | "Approved by admin@example.com" |
| Application rejected | LoanApplication | "Rejected. Reason: Insufficient income" |
| Loan funded | LoanApplication | "Funded by ABC Lending. £25,000 at 8.5%" |
| Payment recorded | RepaymentSchedule | "Payment of £450.00 recorded" |
| Document uploaded | ApplicationDocument | "ID verification document uploaded" |
| Document verified | ApplicationDocument | "Document verified by CRM manager" |
| User created | User | "New borrower account created" |
| Role assigned | User | "Lender role assigned to user" |
| Account suspended | User | "Account suspended for policy violation" |
| Product created | LoanProduct | "New product: Personal Loan 8.5%" |

---

## Query: GetAuditTrailQuery

```csharp
// src/LoanSuperMarket.Application/Features/Audit/GetAuditLogs/GetAuditLogsQuery.cs
using LoanSuperMarket.Shared.Audit;
using MediatR;

namespace LoanSuperMarket.Application.Features.Audit.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? EntityName = null,
    Guid? EntityId = null,
    int Take = 20
) : IRequest<IReadOnlyList<AuditLogDto>>;
```

```csharp
public sealed class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    private readonly IAuditLogRepository _repository;

    public async Task<IReadOnlyList<AuditLogDto>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.EntityName) && request.EntityId.HasValue)
        {
            return await _repository.GetByEntityAsync(
                request.EntityName, request.EntityId.Value, cancellationToken);
        }

        return await _repository.GetRecentAsync(request.Take, cancellationToken);
    }
}
```

---

## Blazor API Client

```csharp
// DashboardApiClient.cs
public async Task<ApiResponse<IReadOnlyList<AuditLogDto>>?> GetAuditTrailAsync(
    string entityName,
    Guid entityId,
    CancellationToken cancellationToken = default)
{
    return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AuditLogDto>>>(
        $"api/dashboard/audit/{entityName}/{entityId}",
        cancellationToken);
}
```

---

## Blazor Component: AuditTimeline

```razor
@inject DashboardApiClient DashboardApi

@if (_isLoading)
{
    <LoadingSkeleton Variant="content" Rows="4" />
}
else if (_auditLogs is not null && _auditLogs.Count > 0)
{
    <div class="relative pl-6 border-l-2 border-slate-200 space-y-6">
        @foreach (var log in _auditLogs)
        {
            <div class="relative">
                <div class="absolute -left-[1.6rem] top-1 h-3 w-3 rounded-full
                            @GetDotColor(log.Action) border-2 border-white"></div>
                <div class="rounded-lg bg-slate-50 p-3">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-semibold text-slate-700">@log.Action</span>
                        <span class="text-xs text-slate-400">
                            @log.OccurredAtUtc.ToString("dd MMM yyyy HH:mm")
                        </span>
                    </div>
                    <p class="text-sm text-slate-600 mt-1">@log.Description</p>
                    <p class="text-xs text-slate-400 mt-1">by @log.PerformedBy</p>
                </div>
            </div>
        }
    </div>
}
else
{
    <EmptyState Icon="📋" Title="No audit history" Message="No actions recorded yet." />
}

@code {
    [Parameter] public string EntityName { get; set; } = string.Empty;
    [Parameter] public Guid EntityId { get; set; }

    private IReadOnlyList<AuditLogDto>? _auditLogs;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(EntityName) && EntityId != Guid.Empty)
        {
            var response = await DashboardApi.GetAuditTrailAsync(EntityName, EntityId);
            _auditLogs = response?.Data;
        }
        _isLoading = false;
    }

    private static string GetDotColor(string action) => action.ToLowerInvariant() switch
    {
        "approved" or "funded" => "bg-green-500",
        "rejected" or "defaulted" => "bg-red-500",
        "submitted" or "created" => "bg-blue-500",
        _ => "bg-slate-400"
    };
}
```

**Usage in a page:**
```razor
<AuditTimeline EntityName="LoanApplication" EntityId="@applicationId" />
```

---

## Step-by-Step Extension Guide

### Adding audit to a new command handler

1. Inject `IAuditLogRepository` and `ICurrentUserService`
2. After the business logic succeeds, create the audit entry:
   ```csharp
   var audit = AuditLog.Create(
       entityName: "MyEntity",
       entityId: entity.Id,
       action: "Updated",
       description: "Description of what changed.",
       performedBy: _currentUser.Email ?? "System");
   await _auditRepo.AddAsync(audit, ct);
   await _auditRepo.SaveChangesAsync(ct);
   ```

### Adding a full audit log page (admin)

1. Create `Pages/AuditLogs.razor` with `@page "/audit-logs"`
2. Use `AuditLogsApiClient.GetRecentAsync(take: 50)`
3. Display in a table with filters for entity name and date range

---

## Common Pitfalls

1. **Forgetting SaveChangesAsync** — `AddAsync` only stages the entity. You must call `SaveChangesAsync`.
2. **Auditing before the action succeeds** — Always audit AFTER the business operation completes successfully.
3. **Missing performer** — Always pass `_currentUser.Email`. For background services, use "System".
4. **Performance** — Audit writes are synchronous with the command. For high-throughput systems, consider async audit via a message queue.
