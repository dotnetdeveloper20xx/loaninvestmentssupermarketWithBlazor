# 16 — Loan Application Domain Logic

## Feature Requirements

The LoanApplication entity is the core of the platform — it represents a borrower's request for funding. Key requirements:

1. **Draft Creation**: Borrowers create drafts with amount, term, and purpose (no product yet)
2. **Full Creation**: Alternative path with product already selected (direct submission)
3. **Full State Machine**: Draft → Submitted → UnderReview → Approved/Rejected/DocumentsRequested → Funded
4. **Product Selection**: Draft applications can have a product associated
5. **Document Management**: Applications have associated `ApplicationDocument` entities
6. **Review Workflow**: Admins can approve, reject, or request additional documents

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| State Machine | Explicit status transitions with guard clauses |
| Factory Methods | `CreateDraft()` and `Create()` for different entry points |
| Value Objects | `Money` for requested amount |
| Aggregate Root | `LoanApplication` owns `ApplicationDocument` collection |

---

## Domain Layer: `LoanApplication.cs`

### Full Source Code

```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;

namespace LoanSuperMarket.Domain.Entities;

public sealed class LoanApplication : AuditableEntity
{
    private LoanApplication()
    {
        Purpose = string.Empty;
        RequestedAmount = Money.Create(1);
    }

    // Constructor for full creation (with product)
    private LoanApplication(
        Guid borrowerId, Guid loanProductId,
        Money requestedAmount, int termMonths, string purpose)
    {
        BorrowerId = borrowerId;
        LoanProductId = loanProductId;
        RequestedAmount = requestedAmount;
        TermMonths = termMonths;
        Purpose = purpose;
        Status = LoanApplicationStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    // Constructor for draft creation (no product yet)
    private LoanApplication(
        Guid borrowerId, Money requestedAmount,
        int termMonths, string purpose)
    {
        BorrowerId = borrowerId;
        LoanProductId = null;
        RequestedAmount = requestedAmount;
        TermMonths = termMonths;
        Purpose = purpose;
        Status = LoanApplicationStatus.Draft;
        SubmittedAtUtc = null;
    }

    public Guid BorrowerId { get; private set; }
    public Guid? LoanProductId { get; private set; }
    public Money RequestedAmount { get; private set; }
    public int TermMonths { get; private set; }
    public string Purpose { get; private set; }
    public LoanApplicationStatus Status { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? DocumentRequestNote { get; private set; }
    public ICollection<ApplicationDocument> Documents { get; private set; } = [];

    // === FACTORY METHODS ===

    public static LoanApplication Create(
        Guid borrowerId, Guid loanProductId,
        Money requestedAmount, int termMonths, string purpose)
    {
        if (borrowerId == Guid.Empty)
            throw new DomainException("Borrower id is required.");
        if (loanProductId == Guid.Empty)
            throw new DomainException("Loan product id is required.");
        if (requestedAmount.Amount <= 0)
            throw new DomainException("Requested amount must be greater than zero.");
        if (termMonths <= 0)
            throw new DomainException("Term must be greater than zero.");
        if (string.IsNullOrWhiteSpace(purpose))
            throw new DomainException("Loan purpose is required.");
        if (purpose.Length > 1000)
            throw new DomainException("Loan purpose cannot exceed 1000 characters.");

        return new LoanApplication(borrowerId, loanProductId,
            requestedAmount, termMonths, purpose.Trim());
    }

    public static LoanApplication CreateDraft(
        Guid borrowerId, decimal requestedAmount,
        int termMonths, string purpose)
    {
        if (borrowerId == Guid.Empty)
            throw new DomainException("Borrower id is required.");
        if (requestedAmount <= 0)
            throw new DomainException("Requested amount must be greater than zero.");
        if (termMonths <= 0)
            throw new DomainException("Term must be greater than zero.");
        if (string.IsNullOrWhiteSpace(purpose))
            throw new DomainException("Loan purpose is required.");
        if (purpose.Length > 1000)
            throw new DomainException("Loan purpose cannot exceed 1000 characters.");

        return new LoanApplication(borrowerId,
            Money.Create(requestedAmount), termMonths, purpose.Trim());
    }

    // === STATE TRANSITIONS ===

    public void SelectProduct(Guid loanProductId)
    {
        if (Status != LoanApplicationStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot select a product when application is in '{Status}' status.");
        if (loanProductId == Guid.Empty)
            throw new DomainException("Loan product id is required.");

        LoanProductId = loanProductId;
        MarkUpdated();
    }

    public void Submit()
    {
        if (Status != LoanApplicationStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot submit application when it is in '{Status}' status.");
        if (LoanProductId is null || LoanProductId == Guid.Empty)
            throw new InvalidOperationException(
                "Cannot submit application without a selected loan product.");

        Status = LoanApplicationStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkUnderReview()
    {
        if (Status != LoanApplicationStatus.Submitted)
            throw new InvalidOperationException(
                $"Cannot move to under review from '{Status}' status.");

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Approve(string reason, string reviewedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
            throw new InvalidOperationException(
                $"Cannot approve from '{Status}' status.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Approval reason is required.");
        if (string.IsNullOrWhiteSpace(reviewedBy))
            throw new DomainException("Reviewer identity is required.");

        Status = LoanApplicationStatus.Approved;
        ReviewReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reject(string reason, string reviewedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
            throw new InvalidOperationException(
                $"Cannot reject from '{Status}' status.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");
        if (string.IsNullOrWhiteSpace(reviewedBy))
            throw new DomainException("Reviewer identity is required.");

        Status = LoanApplicationStatus.Rejected;
        ReviewReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void RequestDocuments(string note, string requestedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
            throw new InvalidOperationException(
                $"Cannot request documents from '{Status}' status.");
        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Document request note is required.");
        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new DomainException("Requester identity is required.");

        Status = LoanApplicationStatus.DocumentsRequested;
        DocumentRequestNote = note;
        ReviewedBy = requestedBy;
        MarkUpdated();
    }

    public void ResubmitForReview()
    {
        if (Status != LoanApplicationStatus.DocumentsRequested)
            throw new InvalidOperationException(
                $"Cannot resubmit from '{Status}' status.");

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Fund()
    {
        if (Status != LoanApplicationStatus.Approved)
            throw new InvalidOperationException(
                $"Cannot fund from '{Status}' status.");

        Status = LoanApplicationStatus.Funded;
        MarkUpdated();
    }

    public void Withdraw()
    {
        if (Status != LoanApplicationStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot withdraw from '{Status}' status.");

        Status = LoanApplicationStatus.Withdrawn;
        MarkUpdated();
    }
}
```

---

## Complete State Machine Diagram

```
                    ┌──────────┐
                    │   Draft   │
                    └─────┬────┘
                          │
              ┌───────────┼───────────┐
              │ Submit()  │           │ Withdraw()
              ▼           │           ▼
        ┌───────────┐    │    ┌───────────┐
        │ Submitted  │    │    │ Withdrawn  │ (Terminal)
        └─────┬─────┘    │    └───────────┘
              │           │
              │ MarkUnderReview()
              ▼
        ┌─────────────┐
        │ UnderReview   │
        └──┬────┬────┬─┘
           │    │    │
  Approve()│    │    │ RequestDocuments()
           │    │    │
           ▼    │    ▼
    ┌──────────┐│  ┌────────────────────┐
    │ Approved  ││  │ DocumentsRequested  │
    └─────┬────┘│  └──────────┬─────────┘
          │     │             │
          │     │ Reject()    │ ResubmitForReview()
          │     ▼             │
          │  ┌──────────┐    │
          │  │ Rejected  │   │ (loops back to UnderReview)
          │  └──────────┘    │
          │                   │
          │ Fund()            │
          ▼                   │
    ┌──────────┐              │
    │  Funded   │             │
    └──────────┘              │
```

---

## ApplicationDocument Entity

```csharp
public sealed class ApplicationDocument : AuditableEntity
{
    public Guid LoanApplicationId { get; private set; }
    public DocumentType Type { get; private set; }
    public string FileName { get; private set; }
    public string StorageReference { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? VerifiedBy { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public string? RejectionNote { get; private set; }

    public static ApplicationDocument Create(
        Guid loanApplicationId, DocumentType type,
        string fileName, string storageReference)
    {
        // Validates all required fields
        return new ApplicationDocument(loanApplicationId, type, fileName, storageReference);
    }

    public void Verify(string verifiedBy) { /* Pending → Verified */ }
    public void Reject(string rejectedBy, string rejectionNote) { /* Pending → Rejected */ }
}
```

### Document Lifecycle

```
Pending → Verified (by reviewer)
Pending → Rejected (by reviewer, with note)
```

---

## Key Business Rules Summary

| Rule | Enforced By |
|---|---|
| Cannot submit without a product | `Submit()` checks `LoanProductId` |
| Only drafts can be submitted | `Submit()` checks `Status == Draft` |
| Only submitted apps go under review | `MarkUnderReview()` guard |
| Only under-review apps can be approved/rejected | `Approve()`/`Reject()` guards |
| Only approved apps can be funded | `Fund()` guard |
| Only drafts can be withdrawn | `Withdraw()` guard |
| Documents requested loops back | `ResubmitForReview()` → UnderReview |
| Reviewer identity is always recorded | Required parameter on Approve/Reject/RequestDocuments |

---

## Step-by-Step Guide: Adding a New Status

Example: Adding `Disbursed` status after `Funded`.

1. **Enum** — Add to `LoanApplicationStatus`:
```csharp
Disbursed = 7
```

2. **Domain** — Add transition method:
```csharp
public void MarkDisbursed()
{
    if (Status != LoanApplicationStatus.Funded)
        throw new InvalidOperationException("Only funded applications can be disbursed.");
    Status = LoanApplicationStatus.Disbursed;
    MarkUpdated();
}
```

3. **Application** — Create `DisburseLoanApplicationCommand` and handler.

4. **API** — Add endpoint `POST /api/loan-applications/{id}/disburse`.

5. **Blazor** — Add button in the admin loan detail view.


---

## Deep Dive: Factory Method Differences

### `Create()` vs `CreateDraft()`

| Aspect | `Create()` | `CreateDraft()` |
|---|---|---|
| Product required | Yes (`Guid loanProductId`) | No (null) |
| Initial status | `Submitted` | `Draft` |
| `SubmittedAtUtc` | Set to `DateTime.UtcNow` | `null` |
| Amount type | `Money` value object | `decimal` (wrapped internally) |
| Use case | Direct submission (admin) | Wizard flow (borrower) |

### Why Two Factory Methods?

The wizard flow requires a draft state because:
1. Borrower enters parameters (Step 1) → draft created
2. System matches products (Step 2) → draft exists without product
3. Borrower selects product (Step 3) → `SelectProduct()` called
4. Documents uploaded (Step 4) → still in draft
5. Borrower submits (Step 5) → `Submit()` transitions to Submitted

The `Create()` method is for admin-initiated applications where the product is already known.

---

## UpdateParameters Method

```csharp
public void UpdateParameters(decimal amount, int termMonths, string purpose)
{
    if (Status != LoanApplicationStatus.Draft)
        throw new InvalidOperationException(
            $"Cannot update parameters when application is in '{Status}' status.");

    if (amount <= 0)
        throw new DomainException("Requested amount must be greater than zero.");
    if (termMonths <= 0)
        throw new DomainException("Term must be greater than zero.");
    if (string.IsNullOrWhiteSpace(purpose))
        throw new DomainException("Loan purpose is required.");
    if (purpose.Length > 1000)
        throw new DomainException("Loan purpose cannot exceed 1000 characters.");

    RequestedAmount = Money.Create(amount);
    TermMonths = termMonths;
    Purpose = purpose.Trim();
    MarkUpdated();
}
```

This method allows borrowers to modify their draft before submission. The guard ensures only drafts can be modified — once submitted, the parameters are locked.

---

## LoanApplicationStatus Enum

```csharp
namespace LoanSuperMarket.Domain.Enums;

public enum LoanApplicationStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Funded = 5,
    Withdrawn = 6,
    Completed = 7,
    DocumentsRequested = 8
}
```

### Status Meanings

| Status | Description | Who triggers |
|---|---|---|
| Draft | Application created but not submitted | Borrower (wizard) |
| Submitted | Borrower has submitted for review | Borrower |
| UnderReview | Admin is actively reviewing | Admin |
| Approved | Admin approved, awaiting funding | Admin |
| Rejected | Admin rejected with reason | Admin |
| Funded | Lender has funded the loan | Lender |
| Withdrawn | Borrower cancelled the draft | Borrower |
| DocumentsRequested | Admin needs more documents | Admin |

---

## Exception Types

The entity uses two different exception types:

1. **`DomainException`** — Business rule violations (invalid data):
   - "Requested amount must be greater than zero"
   - "Loan purpose is required"

2. **`InvalidOperationException`** — State machine violations (wrong status):
   - "Cannot submit when in 'Funded' status"
   - "Cannot approve when in 'Draft' status"

This distinction helps the global exception middleware return appropriate HTTP status codes:
- `DomainException` → 400 Bad Request
- `InvalidOperationException` → 409 Conflict (or 422 Unprocessable Entity)

---

## How Documents Connect

The `LoanApplication` entity has a navigation property:

```csharp
public ICollection<ApplicationDocument> Documents { get; private set; } = [];
```

Documents are managed through the `WizardController`:
- Upload: Creates `ApplicationDocument` via `UploadDocumentCommand`
- Remove: Deletes document via `RemoveDocumentCommand`
- List: Returns documents via `GetApplicationDocumentsQuery`

The `Submit()` method does NOT validate document count — this is intentional. Some products may not require documents. Document requirements are enforced at the review stage by the admin.

---

## Integration with Other Features

### Product Matching (Step 2)

```
LoanApplication.RequestedAmount + TermMonths
    → ProductMatchingService.MatchProductsAsync()
        → Filters published products by amount range and term range
        → Adjusts rate by borrower's CreditTier
        → Returns sorted list
```

### Funding (After Approval)

```
LoanApplication.Fund()
    → Status = Funded
    → FundLoanCommandHandler generates RepaymentSchedule
    → Lender.DeductFunds(application.RequestedAmount.Amount)
```

### Review Queue

```
LoanApplication with Status in [Submitted, UnderReview, DocumentsRequested]
    → Appears in GetReviewQueueQuery results
    → Admin can transition status via ReviewQueueController
```
