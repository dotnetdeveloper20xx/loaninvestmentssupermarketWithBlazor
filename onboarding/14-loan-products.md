# 14 — Loan Products

## Feature Requirements

Loan Products define the lending terms that lenders offer on the marketplace. Key requirements:

1. **Creation**: Products are created with title, description, amount range, interest rate, term range, and owning lender
2. **Value Objects**: Uses `Money` for amounts and `InterestRate` for rates — enforcing domain invariants
3. **Lifecycle**: Draft → PendingApproval → Approved → Published → (Archived)
4. **Edit Guards**: Only Draft products can be edited; Published/Archived cannot
5. **Product Matching**: Published products are matched to borrower applications based on amount and term
6. **CRUD + Paging**: Full server-side paginated grid with search, filter, sort

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Value Objects | `Money` and `InterestRate` enforce domain invariants |
| Factory Method | `LoanProduct.Create()` with comprehensive validation |
| State Machine | Draft → PendingApproval → Approved → Published → Archived |
| Strategy Pattern | `ProductMatchingService` matches products to applications |
| Server-Side Paging | `GetPagedAsync` with EF Core `Skip/Take` |

---

## Domain Layer: `LoanProduct.cs`

### Full Source Code

```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;

namespace LoanSuperMarket.Domain.Entities;

public sealed class LoanProduct : AuditableEntity
{
    private LoanProduct()
    {
        Title = string.Empty;
        Description = string.Empty;
        MinimumAmount = Money.Create(0);
        MaximumAmount = Money.Create(0);
        InterestRate = InterestRate.Create(1);
    }

    private LoanProduct(
        string title, string description,
        Money minimumAmount, Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths, int maximumTermMonths,
        Guid lenderId)
    {
        Title = title;
        Description = description;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        InterestRate = interestRate;
        MinimumTermMonths = minimumTermMonths;
        MaximumTermMonths = maximumTermMonths;
        LenderId = lenderId;
        Status = LoanProductStatus.Draft;
    }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public Money MinimumAmount { get; private set; }
    public Money MaximumAmount { get; private set; }
    public InterestRate InterestRate { get; private set; }
    public int MinimumTermMonths { get; private set; }
    public int MaximumTermMonths { get; private set; }
    public Guid LenderId { get; private set; }
    public LoanProductStatus Status { get; private set; }

    public static LoanProduct Create(
        string title, string description,
        Money minimumAmount, Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths, int maximumTermMonths,
        Guid lenderId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Loan product title is required.");
        if (title.Length > 150)
            throw new DomainException("Loan product title cannot exceed 150 characters.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Loan product description is required.");
        if (minimumAmount.Amount <= 0)
            throw new DomainException("Minimum loan amount must be greater than zero.");
        if (maximumAmount.Amount <= 0)
            throw new DomainException("Maximum loan amount must be greater than zero.");
        if (minimumAmount.Currency != maximumAmount.Currency)
            throw new DomainException("Minimum and maximum loan amounts must use the same currency.");
        if (minimumAmount.Amount > maximumAmount.Amount)
            throw new DomainException("Minimum loan amount cannot be greater than maximum loan amount.");
        if (minimumTermMonths <= 0)
            throw new DomainException("Minimum term must be greater than zero.");
        if (maximumTermMonths <= 0)
            throw new DomainException("Maximum term must be greater than zero.");
        if (minimumTermMonths > maximumTermMonths)
            throw new DomainException("Minimum term cannot be greater than maximum term.");
        if (lenderId == Guid.Empty)
            throw new DomainException("Lender id is required.");

        return new LoanProduct(
            title.Trim(), description.Trim(),
            minimumAmount, maximumAmount, interestRate,
            minimumTermMonths, maximumTermMonths, lenderId);
    }

    public void UpdateDetails(
        string title, string description,
        Money minimumAmount, Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths, int maximumTermMonths)
    {
        if (Status is LoanProductStatus.Published or LoanProductStatus.Archived)
            throw new DomainException("Published or archived loan products cannot be edited.");

        // Re-use Create validation by creating a temporary instance
        var updated = Create(title, description, minimumAmount, maximumAmount,
            interestRate, minimumTermMonths, maximumTermMonths, LenderId);

        Title = updated.Title;
        Description = updated.Description;
        MinimumAmount = updated.MinimumAmount;
        MaximumAmount = updated.MaximumAmount;
        InterestRate = updated.InterestRate;
        MinimumTermMonths = updated.MinimumTermMonths;
        MaximumTermMonths = updated.MaximumTermMonths;
        MarkUpdated();
    }

    public void SubmitForApproval()
    {
        if (Status != LoanProductStatus.Draft)
            throw new DomainException("Only draft loan products can be submitted for approval.");
        Status = LoanProductStatus.PendingApproval;
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != LoanProductStatus.PendingApproval)
            throw new DomainException("Only pending loan products can be approved.");
        Status = LoanProductStatus.Approved;
        MarkUpdated();
    }

    public void Publish()
    {
        if (Status != LoanProductStatus.Approved)
            throw new DomainException("Only approved loan products can be published.");
        Status = LoanProductStatus.Published;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == LoanProductStatus.Archived)
            throw new DomainException("Loan product is already archived.");
        Status = LoanProductStatus.Archived;
        MarkUpdated();
    }
}
```

### Key Design Decisions

1. **`UpdateDetails` reuses `Create` validation** — Instead of duplicating validation logic, it creates a temporary instance via `Create()` and copies the validated values. This ensures consistency.

2. **State guards on `UpdateDetails`** — Published products are live in the marketplace; editing them would break existing applications referencing them.

3. **Value Objects enforce invariants** — `Money` ensures non-negative amounts with valid ISO currency codes. `InterestRate` ensures 0 < rate ≤ 100.

---

## Value Objects

### `Money.cs`

```csharp
public sealed class Money : IEquatable<Money>
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "GBP")
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");
        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }
}
```

### `InterestRate.cs`

```csharp
public sealed class InterestRate : IEquatable<InterestRate>
{
    private InterestRate(decimal percentage) { Percentage = percentage; }

    public decimal Percentage { get; }

    public static InterestRate Create(decimal percentage)
    {
        if (percentage <= 0)
            throw new DomainException("Interest rate must be greater than zero.");
        if (percentage > 100)
            throw new DomainException("Interest rate cannot be greater than 100%.");

        return new InterestRate(decimal.Round(percentage, 2));
    }
}
```

---

## Product Matching Algorithm: `ProductMatchingService.cs`

```csharp
public sealed class ProductMatchingService
{
    private readonly ILoanProductRepository _productRepository;
    private readonly ILenderRepository _lenderRepository;

    public ProductMatchingService(
        ILoanProductRepository productRepository,
        ILenderRepository lenderRepository)
    {
        _productRepository = productRepository;
        _lenderRepository = lenderRepository;
    }

    public async Task<IReadOnlyList<MatchedProductDto>> MatchProductsAsync(
        decimal requestedAmount, int requestedTermMonths,
        CreditTier borrowerTier, CancellationToken ct)
    {
        var publishedProducts = await _productRepository.GetPublishedAsync(ct);
        var lenders = await _lenderRepository.GetAllAsync(ct);
        var lenderLookup = lenders.ToDictionary(l => l.Id, l => l.CompanyName);

        var matched = publishedProducts
            .Where(p => p.MinimumAmount.Amount <= requestedAmount
                     && requestedAmount <= p.MaximumAmount.Amount)
            .Where(p => p.MinimumTermMonths <= requestedTermMonths
                     && requestedTermMonths <= p.MaximumTermMonths)
            .Select(p => new MatchedProductDto(
                ProductId: p.Id,
                Title: p.Title,
                LenderName: lenderLookup.GetValueOrDefault(p.LenderId, "Unknown"),
                EffectiveInterestRate: CalculateEffectiveRate(p.InterestRate.Percentage, borrowerTier),
                MinimumAmount: p.MinimumAmount.Amount,
                MaximumAmount: p.MaximumAmount.Amount,
                MinimumTermMonths: p.MinimumTermMonths,
                MaximumTermMonths: p.MaximumTermMonths))
            .OrderBy(m => m.EffectiveInterestRate)
            .ThenByDescending(m => m.MaximumAmount)
            .ToList();

        return matched;
    }

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
}
```

### Matching Algorithm Explained

1. **Filter by amount**: Product's `[MinimumAmount, MaximumAmount]` must contain the requested amount
2. **Filter by term**: Product's `[MinimumTermMonths, MaximumTermMonths]` must contain the requested term
3. **Calculate effective rate**: Base rate + credit tier adjustment (A=+0%, B=+2%, C=+4%)
4. **Sort**: Best rate first, then largest maximum amount (more flexibility)

---

## API Layer: `LoanProductsController.cs`

```csharp
[ApiController]
[Route("api/loan-products")]
[Authorize(Policy = "CanManageProducts")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly ISender _sender;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanProductDto>>>> GetLoanProducts(...)

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLoanProduct(...)

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanProductDto>>> GetLoanProductById(...)

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<string>>> SubmitForApproval(...)

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(...)

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<string>>> Publish(...)

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ApiResponse<string>>> Archive(...)

    [HttpPost("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanProductDto>>>> GetLoanProductsPaged(...)
}
```

### Endpoint Summary

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/loan-products` | List all products |
| POST | `/api/loan-products` | Create new product |
| GET | `/api/loan-products/{id}` | Get product by ID |
| POST | `/api/loan-products/{id}/submit-for-approval` | Submit draft for approval |
| POST | `/api/loan-products/{id}/approve` | Approve pending product |
| POST | `/api/loan-products/{id}/publish` | Publish approved product |
| POST | `/api/loan-products/{id}/archive` | Archive any product |
| POST | `/api/loan-products/paged` | Server-side paginated query |

---

## Lifecycle State Diagram

```
┌──────────┐
│   Draft   │ ← Create() / UpdateDetails() allowed here
└─────┬────┘
      │ SubmitForApproval()
      ▼
┌──────────────────┐
│ PendingApproval   │
└─────┬────────────┘
      │ Approve()
      ▼
┌──────────┐
│ Approved  │
└─────┬────┘
      │ Publish()
      ▼
┌──────────┐
│ Published │ ← Visible to borrowers, used in matching
└─────┬────┘
      │ Archive()
      ▼
┌──────────┐
│ Archived  │ (Terminal — no longer matched)
└──────────┘
```

---

## Step-by-Step Guide: Adding a New Product Field

Example: Adding `RequiresCollateral` boolean field.

1. **Domain** — Add property to `LoanProduct`:
```csharp
public bool RequiresCollateral { get; private set; }
```

2. **Domain** — Update private constructor and `Create()` factory to accept the new parameter.

3. **Infrastructure** — Update EF Core configuration in `LoanProductConfiguration.cs`.

4. **Infrastructure** — Add migration: `dotnet ef migrations add AddRequiresCollateral`.

5. **Shared** — Update `CreateLoanProductRequest` and `LoanProductDto`.

6. **Application** — Update `CreateLoanProductCommand` and handler.

7. **API** — No changes needed (command already maps from request).

8. **Blazor** — Add checkbox to `CreateLoanProductModal` and column to grid.
