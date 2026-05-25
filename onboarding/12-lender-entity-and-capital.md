# 12 — Lender Entity & Capital Management

## Feature Requirements

The Lender entity represents a financial institution or individual investor who provides capital to fund loans on the platform. Key requirements:

1. **Registration**: Lenders register with company name, contact details, and initial available funds
2. **Verification Lifecycle**: Lenders progress through PendingVerification → Verified → (optionally Suspended/Archived)
3. **Capital Management**: Lenders can top up funds and have funds deducted when they fund a loan
4. **User Linking**: Each lender profile is linked to an ASP.NET Identity user via `UserId`
5. **Admin Management**: Admins can view, search, sort, and filter lenders through a data grid

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Domain-Driven Design | Rich entity with encapsulated business rules |
| Factory Method Pattern | `Lender.Create()` static factory with validation |
| State Machine | Status transitions with guard clauses |
| Repository Pattern | `ILenderRepository` abstraction over EF Core |
| CQRS + MediatR | Separate command/query handlers |
| Blazor WASM | Client-side data grid with search/sort/filter |

## Architecture Flow

```
Blazor Page (Lenders.razor)
    → LendersApiClient (HttpClient)
        → LendersController (ASP.NET Core)
            → MediatR (Send command/query)
                → CreateLenderCommandHandler / GetLendersQueryHandler
                    → ILenderRepository
                        → LenderRepository (EF Core)
                            → ApplicationDbContext → SQL Server
```

---

## Domain Layer: `Lender.cs`

The `Lender` entity lives in `LoanSuperMarket.Domain.Entities` and encapsulates all business rules for lender management.

### Full Source Code

```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class Lender : AuditableEntity
{
    private Lender()
    {
        CompanyName = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    private Lender(
        string companyName,
        string contactName,
        string email,
        string phoneNumber,
        decimal availableFunds)
    {
        CompanyName = companyName;
        ContactName = contactName;
        Email = email;
        PhoneNumber = phoneNumber;
        AvailableFunds = availableFunds;
        Status = LenderStatus.PendingVerification;
    }

    public string CompanyName { get; private set; }
    public string ContactName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public decimal AvailableFunds { get; private set; }
    public LenderStatus Status { get; private set; }
    public string? UserId { get; private set; }

    public static Lender Create(
        string companyName,
        string contactName,
        string email,
        string phoneNumber,
        decimal availableFunds)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("Company name is required.");

        if (string.IsNullOrWhiteSpace(contactName))
            throw new DomainException("Contact name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Phone number is required.");

        if (availableFunds < 0)
            throw new DomainException("Available funds cannot be negative.");

        return new Lender(
            companyName.Trim(),
            contactName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            decimal.Round(availableFunds, 2));
    }

    public void Verify()
    {
        if (Status != LenderStatus.PendingVerification)
            throw new DomainException("Only pending lenders can be verified.");

        Status = LenderStatus.Verified;
        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status == LenderStatus.Archived)
            throw new DomainException("Archived lenders cannot be suspended.");

        Status = LenderStatus.Suspended;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == LenderStatus.Archived)
            throw new DomainException("Lender is already archived.");

        Status = LenderStatus.Archived;
        MarkUpdated();
    }

    public void DeductFunds(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Deduction amount must be greater than zero.");

        if (amount > AvailableFunds)
            throw new DomainException(
                "Insufficient funds. The deduction amount exceeds available funds.");

        AvailableFunds -= amount;
        MarkUpdated();
    }

    public void TopUpFunds(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Top-up amount must be greater than zero.");

        AvailableFunds += amount;
        MarkUpdated();
    }
}
```

### Method-by-Method Explanation

#### `Lender.Create()` — Factory Method

- **Purpose**: Only way to instantiate a valid `Lender`. Constructors are `private`.
- **Validation**: Checks all required fields are non-empty, funds are non-negative.
- **Normalization**: Trims whitespace, lowercases email, rounds funds to 2 decimal places.
- **Initial State**: Always starts as `LenderStatus.PendingVerification`.

#### `Verify()` — Status Transition

- **Guard**: Only lenders in `PendingVerification` can be verified.
- **Effect**: Sets status to `Verified`, calls `MarkUpdated()` (sets `UpdatedAtUtc`).

#### `Suspend()` — Status Transition

- **Guard**: Archived lenders cannot be suspended (terminal state).
- **Effect**: Sets status to `Suspended` from any non-archived state.

#### `Archive()` — Terminal State

- **Guard**: Prevents double-archiving (idempotency check).
- **Effect**: Sets status to `Archived`. This is a terminal state.

#### `DeductFunds(decimal amount)` — Capital Withdrawal

- **Guard 1**: Amount must be positive.
- **Guard 2**: Amount cannot exceed `AvailableFunds` (insufficient funds check).
- **Effect**: Subtracts amount from `AvailableFunds`.
- **Usage**: Called by `FundLoanCommandHandler` when a lender funds a loan.

#### `TopUpFunds(decimal amount)` — Capital Deposit

- **Guard**: Amount must be positive.
- **Effect**: Adds amount to `AvailableFunds`.
- **Usage**: Called by `TopUpFundsCommand` when a lender adds capital.

---

## Infrastructure Layer: `LenderRepository.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.Lenders;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class LenderRepository : ILenderRepository
{
    private readonly ApplicationDbContext _context;

    public LenderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Lender lender, CancellationToken cancellationToken)
    {
        await _context.Lenders.AddAsync(lender, cancellationToken);
    }

    public async Task<Lender?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Lender?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Lender>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .AnyAsync(x => x.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LenderDto>> GetPagedAsync(
        GridQueryRequest request, CancellationToken cancellationToken)
    {
        // Pagination defaults
        request.PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        request.PageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = _context.Lenders.AsNoTracking();

        // Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();
            query = query.Where(lender =>
                lender.CompanyName.Contains(searchText)
                || lender.ContactName.Contains(searchText)
                || lender.Email.Contains(searchText)
                || lender.PhoneNumber.Contains(searchText));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(lender =>
                lender.Status.ToString() == request.Status);
        }

        // Dynamic sorting via pattern matching
        query = (request.SortColumn, request.SortDirection) switch
        {
            ("CompanyName", SortDirection.Asc) => query.OrderBy(x => x.CompanyName),
            ("CompanyName", SortDirection.Desc) => query.OrderByDescending(x => x.CompanyName),
            ("AvailableFunds", SortDirection.Asc) => query.OrderBy(x => x.AvailableFunds),
            ("AvailableFunds", SortDirection.Desc) => query.OrderByDescending(x => x.AvailableFunds),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(lender => new LenderDto
            {
                Id = lender.Id,
                CompanyName = lender.CompanyName,
                ContactName = lender.ContactName,
                Email = lender.Email,
                PhoneNumber = lender.PhoneNumber,
                AvailableFunds = lender.AvailableFunds,
                Status = lender.Status.ToString(),
                CreatedAtUtc = lender.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LenderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
```

### Key Methods Explained

| Method | Purpose |
|---|---|
| `AddAsync` | Adds a new lender to the EF Core change tracker |
| `GetByIdAsync` | Retrieves a lender by primary key |
| `GetByUserIdAsync` | Links Identity user to lender profile — used by `FundingController` |
| `GetAllAsync` | Returns all lenders ordered by creation date |
| `EmailExistsAsync` | Duplicate email check during registration |
| `SaveChangesAsync` | Persists all tracked changes to the database |
| `GetPagedAsync` | Server-side pagination with search, status filter, and dynamic sorting |

---

## API Layer: `LendersController.cs`

```csharp
[ApiController]
[Route("api/lenders")]
[Authorize(Policy = "CanManageLenders")]
public sealed class LendersController : ControllerBase
{
    private readonly ISender _sender;

    public LendersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LenderDto>>>> GetLenders(
        CancellationToken cancellationToken)
    {
        var lenders = await _sender.Send(new GetLendersQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LenderDto>>.Ok(
            lenders, "Lenders retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLender(
        [FromBody] CreateLenderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLenderCommand(
            request.CompanyName,
            request.ContactName,
            request.Email,
            request.PhoneNumber,
            request.AvailableFunds);

        var lenderId = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(lenderId, "Lender created successfully."));
    }
}
```

### Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/lenders` | List all lenders |
| POST | `/api/lenders` | Create a new lender |

### Authorization

The controller uses `[Authorize(Policy = "CanManageLenders")]` — only users with the appropriate permission can access these endpoints.

---

## Blazor Frontend: `Lenders.razor`

The Lenders page provides a full data grid with:
- Search across company name, contact, email, phone
- Status filter dropdown (PendingVerification, Verified, Suspended, Archived)
- Sortable column headers
- Pagination
- Create modal
- Quick-view drawer

### Key Code Sections

```razor
@page "/lenders"
@inject LendersApiClient LendersApiClient
@inject DrawerService DrawerService

<PageHeader Title="Lenders"
            Subtitle="Manage lender profiles, verification status and available funding capacity.">
    <ActionContent>
        <button @onclick="OpenCreateModal">+ New Lender</button>
    </ActionContent>
</PageHeader>

<DataGridToolbar State="_gridState"
                 StatusOptions="_statusOptions"
                 OnStateChanged="HandleGridStateChanged" />

<AppDataTable IsLoading="_isLoading" IsEmpty="@(!_isLoading && FilteredLenders.Count == 0)">
    <!-- Column headers with DataGridColumnHeader for sorting -->
    <!-- Row template iterating PagedLenders -->
</AppDataTable>

<DataGridPager State="_gridState"
               TotalCount="FilteredLenders.Count"
               OnPageChanged="HandleGridStateChanged" />
```

### State Management

```csharp
@code {
    private readonly GridState _gridState = new();
    private List<LenderDto> _lenders = [];
    private bool _isLoading = true;

    // Client-side filtering and sorting
    private List<LenderDto> FilteredLenders { get { /* LINQ filter/sort */ } }

    // Pagination
    private List<LenderDto> PagedLenders =>
        FilteredLenders
            .Skip((_gridState.PageNumber - 1) * _gridState.PageSize)
            .Take(_gridState.PageSize)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadLendersAsync();
    }
}
```

---

## How Layers Connect

1. **User clicks "New Lender"** → `OpenCreateModal()` sets `_isCreateModalOpen = true`
2. **User fills form and submits** → `CreateLenderModal` calls `LendersApiClient.CreateLenderAsync()`
3. **API Client** sends `POST /api/lenders` with `CreateLenderRequest` JSON body
4. **Controller** maps request to `CreateLenderCommand` and sends via MediatR
5. **Handler** calls `Lender.Create()` factory, then `_repository.AddAsync()` + `SaveChangesAsync()`
6. **Domain entity** validates all inputs, normalizes data, sets initial status
7. **Response** flows back: Handler → Controller → API Client → Modal → Page refreshes grid

---

## Step-by-Step Guide: Extending the Feature

### Adding a "Reactivate" method to Lender

1. **Domain Layer** — Add method to `Lender.cs`:
```csharp
public void Reactivate()
{
    if (Status != LenderStatus.Suspended)
        throw new DomainException("Only suspended lenders can be reactivated.");

    Status = LenderStatus.Verified;
    MarkUpdated();
}
```

2. **Application Layer** — Create command:
```csharp
// Features/Lenders/ReactivateLender/ReactivateLenderCommand.cs
public sealed record ReactivateLenderCommand(Guid LenderId) : IRequest<Unit>;
```

3. **Application Layer** — Create handler:
```csharp
public sealed class ReactivateLenderCommandHandler : IRequestHandler<ReactivateLenderCommand, Unit>
{
    private readonly ILenderRepository _repository;

    public async Task<Unit> Handle(ReactivateLenderCommand request, CancellationToken ct)
    {
        var lender = await _repository.GetByIdAsync(request.LenderId, ct)
            ?? throw new DomainException("Lender not found.");

        lender.Reactivate();
        await _repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

4. **API Layer** — Add endpoint to `LendersController`:
```csharp
[HttpPost("{id:guid}/reactivate")]
public async Task<ActionResult<ApiResponse<string>>> Reactivate(Guid id, CancellationToken ct)
{
    await _sender.Send(new ReactivateLenderCommand(id), ct);
    return Ok(ApiResponse<string>.Ok("Lender reactivated.", "Action completed."));
}
```

5. **Blazor** — Add button in the Lenders page row template for suspended lenders.

---

## LenderStatus Enum

```csharp
namespace LoanSuperMarket.Domain.Enums;

public enum LenderStatus
{
    PendingVerification,
    Verified,
    Suspended,
    Archived
}
```

### State Diagram

```
┌─────────────────────┐
│ PendingVerification  │
└──────────┬──────────┘
           │ Verify()
           ▼
┌─────────────────────┐
│      Verified        │◄──── (Reactivate from Suspended)
└──────────┬──────────┘
           │ Suspend()
           ▼
┌─────────────────────┐
│      Suspended       │
└──────────┬──────────┘
           │ Archive()
           ▼
┌─────────────────────┐
│      Archived        │ (Terminal — no transitions out)
└─────────────────────┘
```

Note: `Archive()` can be called from any non-archived state. `Suspend()` can be called from any non-archived state.
