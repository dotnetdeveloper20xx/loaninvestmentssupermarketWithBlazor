# 13 — Borrower Entity & Verification

## Feature Requirements

The Borrower entity represents an individual seeking a loan on the platform. Key requirements:

1. **Registration**: Borrowers register with personal details (name, email, phone, date of birth)
2. **Age Validation**: Must be at least 18 years old at time of registration
3. **Verification Lifecycle**: PendingVerification → Verified → (optionally Suspended/Archived)
4. **Credit Tier Assignment**: Borrowers are assigned a credit tier (A, B, C) that affects interest rates
5. **User Linking**: Each borrower profile links to an ASP.NET Identity user via `UserId`
6. **Admin Management**: Admins manage borrowers through a searchable, sortable data grid

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Domain-Driven Design | Rich entity with encapsulated validation |
| Factory Method | `Borrower.Create()` with age validation |
| State Machine | Status transitions with guard clauses |
| Repository Pattern | `IBorrowerRepository` abstraction |
| CQRS + MediatR | Command/query separation |
| Blazor WASM | Client-side data grid |

---

## Domain Layer: `Borrower.cs`

### Full Source Code

```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class Borrower : AuditableEntity
{
    private Borrower()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    private Borrower(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateTime dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Status = BorrowerStatus.PendingVerification;
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public BorrowerStatus Status { get; private set; }
    public CreditTier? CreditTier { get; private set; }
    public string? UserId { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    public static Borrower Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Phone number is required.");

        if (dateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-18))
            throw new DomainException("Borrower must be at least 18 years old.");

        return new Borrower(
            firstName.Trim(),
            lastName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            dateOfBirth.Date);
    }

    public void Verify()
    {
        if (Status != BorrowerStatus.PendingVerification)
            throw new DomainException("Only pending borrowers can be verified.");

        Status = BorrowerStatus.Verified;
        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status == BorrowerStatus.Archived)
            throw new DomainException("Archived borrowers cannot be suspended.");

        Status = BorrowerStatus.Suspended;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == BorrowerStatus.Archived)
            throw new DomainException("Borrower is already archived.");

        Status = BorrowerStatus.Archived;
        MarkUpdated();
    }
}
```

### Method-by-Method Explanation

#### `Borrower.Create()` — Factory Method

- **Age Validation**: The critical business rule — `dateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-18)` checks if the person is under 18. If their DOB is after the date 18 years ago, they're too young.
- **Normalization**: Trims all strings, lowercases email, stores only the date portion of DOB.
- **Initial State**: Always `BorrowerStatus.PendingVerification`.
- **CreditTier**: Starts as `null` — assigned later by admin or credit scoring system.

#### `Verify()` — Verification Gate

- **Guard**: Only `PendingVerification` borrowers can be verified.
- **Business Context**: Admin reviews borrower documents/identity before verifying.
- **Effect**: Status → `Verified`. Only verified borrowers can apply for loans.

#### `Suspend()` — Temporary Deactivation

- **Guard**: Cannot suspend already-archived borrowers.
- **Business Context**: Used when fraud is suspected or borrower violates terms.

#### `Archive()` — Permanent Deactivation

- **Guard**: Idempotency check — already archived throws.
- **Business Context**: Borrower account permanently closed.

### CreditTier Enum

```csharp
namespace LoanSuperMarket.Domain.Enums;

public enum CreditTier
{
    A,  // Best rate (base rate + 0%)
    B,  // Medium risk (base rate + 2%)
    C   // Higher risk (base rate + 4%)
}
```

Credit tier directly affects the interest rate a borrower pays. The `ProductMatchingService` and `FundLoanCommandHandler` both use this formula:

```csharp
private static decimal CalculateEffectiveRate(decimal baseRate, CreditTier tier)
{
    return tier switch
    {
        CreditTier.A => baseRate,
        CreditTier.B => baseRate + 2m,
        CreditTier.C => baseRate + 4m,
        _ => baseRate
    };
}
```

---

## Infrastructure Layer: `BorrowerRepository.cs`

```csharp
public sealed class BorrowerRepository : IBorrowerRepository
{
    private readonly ApplicationDbContext _context;

    public BorrowerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Borrower borrower, CancellationToken cancellationToken)
    {
        await _context.Borrowers.AddAsync(borrower, cancellationToken);
    }

    public async Task<Borrower?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Borrower?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Borrower>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .AnyAsync(x => x.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<BorrowerDto>> GetPagedAsync(
        GridQueryRequest request, CancellationToken cancellationToken)
    {
        request.PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        request.PageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = _context.Borrowers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();
            query = query.Where(borrower =>
                borrower.FirstName.Contains(searchText)
                || borrower.LastName.Contains(searchText)
                || borrower.Email.Contains(searchText)
                || borrower.PhoneNumber.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(borrower =>
                borrower.Status.ToString() == request.Status);
        }

        query = (request.SortColumn, request.SortDirection) switch
        {
            ("FullName", SortDirection.Asc) =>
                query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            ("FullName", SortDirection.Desc) =>
                query.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName),
            ("Email", SortDirection.Asc) => query.OrderBy(x => x.Email),
            ("Email", SortDirection.Desc) => query.OrderByDescending(x => x.Email),
            ("DateOfBirth", SortDirection.Asc) => query.OrderBy(x => x.DateOfBirth),
            ("DateOfBirth", SortDirection.Desc) => query.OrderByDescending(x => x.DateOfBirth),
            ("Status", SortDirection.Asc) => query.OrderBy(x => x.Status),
            ("Status", SortDirection.Desc) => query.OrderByDescending(x => x.Status),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(borrower => new BorrowerDto
            {
                Id = borrower.Id,
                FirstName = borrower.FirstName,
                LastName = borrower.LastName,
                FullName = borrower.FirstName + " " + borrower.LastName,
                Email = borrower.Email,
                PhoneNumber = borrower.PhoneNumber,
                DateOfBirth = borrower.DateOfBirth,
                Status = borrower.Status.ToString(),
                CreatedAtUtc = borrower.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BorrowerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
```

### `GetByUserIdAsync` — Identity Linking

This method is critical for the wizard flow. When a borrower logs in and starts a loan application, the system:
1. Gets the current user's Identity `UserId` from JWT claims
2. Calls `GetByUserIdAsync(userId)` to find their borrower profile
3. Uses the borrower's `Id` (domain entity GUID) for all loan operations

---

## API Layer: `BorrowersController.cs`

```csharp
[ApiController]
[Route("api/borrowers")]
[Authorize(Policy = "CanManageBorrowers")]
public sealed class BorrowersController : ControllerBase
{
    private readonly ISender _sender;

    public BorrowersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BorrowerDto>>>> GetBorrowers(
        CancellationToken cancellationToken)
    {
        var borrowers = await _sender.Send(new GetBorrowersQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BorrowerDto>>.Ok(
            borrowers, "Borrowers retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBorrower(
        [FromBody] CreateBorrowerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBorrowerCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth);

        var borrowerId = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<Guid>.Ok(borrowerId, "Borrower created successfully."));
    }
}
```

---

## Blazor Frontend: `Borrowers.razor`

The Borrowers page mirrors the Lenders page pattern with:
- Client-side search across name, email, phone
- Status filter (PendingVerification, Verified, Suspended, Archived)
- Sortable columns (FullName, Email, DateOfBirth, Status, CreatedAtUtc)
- Pagination via `DataGridPager`
- Create modal and quick-view drawer

### Key Pattern: Client-Side Filtering

```csharp
private List<BorrowerDto> FilteredBorrowers
{
    get
    {
        var query = _borrowers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_gridState.SearchText))
        {
            query = query.Where(borrower =>
                borrower.FullName.Contains(_gridState.SearchText, StringComparison.OrdinalIgnoreCase)
                || borrower.Email.Contains(_gridState.SearchText, StringComparison.OrdinalIgnoreCase)
                || borrower.PhoneNumber.Contains(_gridState.SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(_gridState.SelectedStatus))
        {
            query = query.Where(borrower =>
                borrower.Status.Equals(_gridState.SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        // Dynamic sorting via pattern matching
        query = (_gridState.SortColumn, _gridState.SortAscending) switch
        {
            ("FullName", true) => query.OrderBy(b => b.FullName),
            ("FullName", false) => query.OrderByDescending(b => b.FullName),
            // ... other columns
            _ => query.OrderByDescending(b => b.CreatedAtUtc)
        };

        return query.ToList();
    }
}
```

---

## State Diagram

```
┌─────────────────────┐
│ PendingVerification  │
└──────────┬──────────┘
           │ Verify()
           ▼
┌─────────────────────┐
│      Verified        │
└──────────┬──────────┘
           │ Suspend()
           ▼
┌─────────────────────┐
│      Suspended       │
└──────────┬──────────┘
           │ Archive()
           ▼
┌─────────────────────┐
│      Archived        │ (Terminal)
└─────────────────────┘
```

---

## Step-by-Step Guide: Adding Credit Tier Assignment

To add an admin action that assigns a credit tier to a borrower:

1. **Domain** — Add method to `Borrower.cs`:
```csharp
public void AssignCreditTier(CreditTier tier)
{
    if (Status != BorrowerStatus.Verified)
        throw new DomainException("Only verified borrowers can be assigned a credit tier.");

    CreditTier = tier;
    MarkUpdated();
}
```

2. **Application** — Create `AssignCreditTierCommand`:
```csharp
public sealed record AssignCreditTierCommand(Guid BorrowerId, CreditTier Tier) : IRequest<Unit>;
```

3. **Application** — Create handler that loads borrower, calls `AssignCreditTier()`, saves.

4. **API** — Add `POST /api/borrowers/{id}/assign-credit-tier` endpoint.

5. **Blazor** — Add dropdown in the borrower drawer to select and assign tier.

---

## How the Borrower Connects to Loan Applications

When a borrower applies for a loan:
1. `WizardController.CreateDraft()` gets the current user's `UserId`
2. `CreateDraftLoanApplicationCommandHandler` calls `_borrowerRepository.GetByUserIdAsync(userId)`
3. The borrower's `Id` is stored on the `LoanApplication.BorrowerId`
4. The borrower's `CreditTier` is used by `ProductMatchingService` to calculate effective rates
5. The borrower's `CreditTier` is used by `FundLoanCommandHandler` to set the funded rate
