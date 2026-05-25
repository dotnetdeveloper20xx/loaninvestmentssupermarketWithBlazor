# Domain Layer Deep Dive

> **Audience:** Developers who know C# but are new to Domain-Driven Design (DDD).  
> **Goal:** After reading this document, you can create new entities, value objects, and understand every state machine in the Loan Investment Supermarket.

---

## Table of Contents

1. [What is Domain-Driven Design (DDD)?](#1-what-is-domain-driven-design-ddd)
2. [Base Classes](#2-base-classes)
3. [Value Objects](#3-value-objects)
4. [Entities with Factory Methods](#4-entities-with-factory-methods)
5. [State Machines](#5-state-machines)
6. [Domain Services](#6-domain-services)
7. [Domain Events (brief)](#7-domain-events-brief)
8. [Key Rules for the Domain Layer](#8-key-rules-for-the-domain-layer)
9. [How to Create a New Entity](#9-how-to-create-a-new-entity)

---

## 1. What is Domain-Driven Design (DDD)?

### The Core Idea

Domain-Driven Design is a software design philosophy that says: **the most important code in your application is the code that models your business rules**. Everything else (databases, APIs, UI) exists to serve the domain.

In practice, this means:

- **Business logic lives in the Domain layer** — not in controllers, not in services that call repositories.
- **The domain model is the single source of truth** for what is and isn't allowed.
- **Code reads like the business** — a product manager should be able to look at your entity and understand the rules.

### Rich Domain Models vs. Anemic Models

An **anemic model** is a class with only properties and no behavior:

```csharp
// ❌ Anemic — logic lives somewhere else
public class LoanProduct
{
    public string Title { get; set; }
    public string Status { get; set; }  // anyone can set this to anything
}
```

A **rich domain model** encapsulates both data AND behavior:

```csharp
// ✅ Rich — the entity protects its own invariants
public sealed class LoanProduct : AuditableEntity
{
    public LoanProductStatus Status { get; private set; }  // private set!

    public void Approve()
    {
        if (Status != LoanProductStatus.PendingApproval)
            throw new DomainException("Only pending loan products can be approved.");
        Status = LoanProductStatus.Approved;
    }
}
```

With rich models, **it is impossible to put the system into an invalid state** because the entity itself refuses illegal transitions.

### Ubiquitous Language

DDD insists that developers and business stakeholders use the **same vocabulary**. In this project, our ubiquitous language includes:

| Term | Meaning |
|------|---------|
| **Borrower** | A person who applies for loans |
| **Lender** | A company that provides funds and creates loan products |
| **LoanProduct** | A template defining loan terms (rate, amount range, term range) |
| **LoanApplication** | A borrower's request for a specific loan |
| **RepaymentSchedule** | The full payment plan generated after funding |
| **Installment** | A single monthly payment within a schedule |
| **Money** | A value object representing an amount + currency |
| **InterestRate** | A value object representing a percentage rate |

These terms appear in class names, method names, and exception messages — never abbreviated, never renamed.

---

## 2. Base Classes

Every entity in the domain inherits from a small hierarchy of base classes. These provide identity, audit tracking, and a custom exception type.

### 2.1 BaseEntity — Identity

```csharp
namespace LoanSuperMarket.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
```

**What it does:**
- Provides a `Guid` primary key that is auto-generated on construction.
- Uses `protected set` so derived classes can override it (e.g., when loading from a database), but external code cannot.
- Every entity in the system has a globally unique identifier from the moment it's created — no need to wait for a database insert.

### 2.2 AuditableEntity — Timestamps and User Tracking

```csharp
namespace LoanSuperMarket.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public string? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string? UpdatedBy { get; private set; }

    public void MarkCreated(string? createdBy = null)
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
```

**What it does:**
- `CreatedAtUtc` — automatically set to `DateTime.UtcNow` when the object is instantiated.
- `CreatedBy` — optional string identifying who created the entity (set via `MarkCreated()`).
- `UpdatedAtUtc` — `null` until the first update; set by `MarkUpdated()`.
- `UpdatedBy` — optional string identifying who last modified the entity.
- `MarkCreated(string?)` — explicitly stamps creation time and user. Called by the Application layer or infrastructure.
- `MarkUpdated(string?)` — stamps the update time. Called inside entity methods after every state change.

**Why it matters:** Every state transition method in our entities calls `MarkUpdated()` at the end. This means the audit trail is maintained automatically — you never forget to update the timestamp because the entity does it itself.

### 2.3 DomainException — Business Rule Violations

```csharp
namespace LoanSuperMarket.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
```

**What it does:**
- A simple, sealed exception class for domain rule violations.
- The message should be a human-readable explanation of what business rule was broken.
- The Application layer catches these and converts them to appropriate HTTP responses (typically 400 Bad Request).

**When to throw it:**
- Invalid input to a factory method: `"Borrower must be at least 18 years old."`
- Illegal state transition: `"Only pending loan products can be approved."`
- Value object constraint violation: `"Interest rate must be greater than zero."`

---

## 3. Value Objects

### What Are Value Objects?

Value objects are **immutable types defined by their values, not by an identity**. Two `Money` objects with the same amount and currency are considered equal — there's no "ID" distinguishing them.

Key characteristics:
1. **Immutable** — once created, they never change. All properties are read-only.
2. **Equality by value** — two instances are equal if all their properties match.
3. **Self-validating** — the factory method rejects invalid data, so if you have a `Money` instance, you know it's valid.
4. **No side effects** — they don't modify external state.

### When to Use Value Objects vs. Primitive Types

| Use a Value Object when... | Use a primitive when... |
|---|---|
| The concept has validation rules (e.g., money can't be negative) | It's a simple identifier with no rules |
| The concept has multiple components (amount + currency) | It's a single value with no constraints |
| You want type safety (can't accidentally pass an interest rate where money is expected) | The meaning is obvious from context |
| The concept appears in multiple entities | It's used in exactly one place |

### 3.1 Money — Full Code and Explanation

```csharp
using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.ValueObjects;

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
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        if (currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO code.");
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public bool Equals(Money? other)
    {
        if (other is null)
        {
            return false;
        }

        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public override string ToString()
    {
        return $"{Currency} {Amount:N2}";
    }
}
```

**Method-by-method breakdown:**

| Member | Purpose |
|--------|---------|
| `private Money(decimal, string)` | Private constructor — forces all creation through the factory. |
| `Amount { get; }` | Read-only property. No setter means immutability. |
| `Currency { get; }` | Read-only 3-letter ISO currency code (e.g., "GBP", "USD"). |
| `Create(decimal, string)` | **Factory method.** Validates: amount ≥ 0, currency not empty, currency is exactly 3 chars. Rounds to 2 decimal places. Normalizes currency to uppercase. Defaults to "GBP". |
| `Equals(Money?)` | Value equality — two Money objects are equal if both Amount and Currency match. |
| `Equals(object?)` | Override for `object.Equals` — delegates to the typed version. |
| `GetHashCode()` | Combines Amount and Currency into a hash. Required when overriding Equals. |
| `ToString()` | Human-readable format: `"GBP 1,500.00"`. |

**Usage example:**
```csharp
var price = Money.Create(5000m);           // GBP 5,000.00
var usd = Money.Create(1000m, "USD");      // USD 1,000.00
var invalid = Money.Create(-1m);           // throws DomainException
```

### 3.2 InterestRate — Full Code and Explanation

```csharp
using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.ValueObjects;

public sealed class InterestRate : IEquatable<InterestRate>
{
    private InterestRate(decimal percentage)
    {
        Percentage = percentage;
    }

    public decimal Percentage { get; }

    public static InterestRate Create(decimal percentage)
    {
        if (percentage <= 0)
        {
            throw new DomainException("Interest rate must be greater than zero.");
        }

        if (percentage > 100)
        {
            throw new DomainException("Interest rate cannot be greater than 100%.");
        }

        return new InterestRate(decimal.Round(percentage, 2));
    }

    public bool Equals(InterestRate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Percentage == other.Percentage;
    }

    public override bool Equals(object? obj)
    {
        return obj is InterestRate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Percentage.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Percentage:N2}%";
    }
}
```

**Method-by-method breakdown:**

| Member | Purpose |
|--------|---------|
| `private InterestRate(decimal)` | Private constructor — creation only through factory. |
| `Percentage { get; }` | Read-only. Represents the annual interest rate as a percentage (e.g., 5.75 means 5.75%). |
| `Create(decimal)` | **Factory method.** Validates: must be > 0 and ≤ 100. Rounds to 2 decimal places. |
| `Equals(InterestRate?)` | Value equality — two rates are equal if their percentages match. |
| `GetHashCode()` | Hash based on the percentage value. |
| `ToString()` | Human-readable: `"5.75%"`. |

**Usage example:**
```csharp
var rate = InterestRate.Create(7.5m);      // 7.50%
var invalid = InterestRate.Create(0m);     // throws: "Interest rate must be greater than zero."
var tooHigh = InterestRate.Create(150m);   // throws: "Interest rate cannot be greater than 100%."
```

---

## 4. Entities with Factory Methods

### Why Private Constructors + Static Create() Factories?

In traditional C#, you'd write:

```csharp
// ❌ Anyone can create an invalid lender
var lender = new Lender();
lender.CompanyName = "";  // oops, empty name
lender.AvailableFunds = -500;  // oops, negative funds
```

With the factory pattern:

```csharp
// ✅ Validation happens at creation time — impossible to create invalid entity
var lender = Lender.Create("Acme Finance", "John Smith", "john@acme.com", "07700900000", 100000m);
```

**Benefits:**
1. **Entities are always valid** — if `Create()` returns, the entity is in a legal state.
2. **Validation is centralized** — all rules live in one place, not scattered across controllers.
3. **Intent is clear** — `Create()` is a named operation, not just "new up an object."
4. **EF Core compatibility** — the parameterless private constructor exists solely for Entity Framework to hydrate objects from the database.

### 4.1 Lender.Create() — Full Walkthrough

```csharp
public sealed class Lender : AuditableEntity
{
    // Parameterless constructor for EF Core
    private Lender()
    {
        CompanyName = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    // Real constructor — only called from Create()
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
        Status = LenderStatus.PendingVerification;  // Initial state!
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
        {
            throw new DomainException("Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(contactName))
        {
            throw new DomainException("Contact name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required.");
        }

        if (availableFunds < 0)
        {
            throw new DomainException("Available funds cannot be negative.");
        }

        return new Lender(
            companyName.Trim(),
            contactName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            decimal.Round(availableFunds, 2));
    }
}
```

**What happens step by step:**

1. **Validate all inputs** — each field is checked for null/empty. Funds must be non-negative.
2. **Normalize data** — strings are trimmed, email is lowercased, funds are rounded to 2 decimal places.
3. **Set initial state** — `Status = LenderStatus.PendingVerification`. A new lender always starts pending.
4. **Return the entity** — the caller gets a fully valid `Lender` instance.

### 4.2 Borrower.Create() — Full Walkthrough (with Age Validation)

```csharp
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
        Status = BorrowerStatus.PendingVerification;  // Initial state!
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
        {
            throw new DomainException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required.");
        }

        // KEY BUSINESS RULE: Must be 18+ to borrow
        if (dateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-18))
        {
            throw new DomainException("Borrower must be at least 18 years old.");
        }

        return new Borrower(
            firstName.Trim(),
            lastName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            dateOfBirth.Date);  // Store date only, no time component
    }
}
```

**Key difference from Lender:** The age validation rule. This is a real business rule — UK financial regulations require borrowers to be at least 18. The domain enforces this, not the UI.

### 4.3 LoanProduct.Create() — Full Walkthrough (with Value Objects)

```csharp
public static LoanProduct Create(
    string title,
    string description,
    Money minimumAmount,
    Money maximumAmount,
    InterestRate interestRate,
    int minimumTermMonths,
    int maximumTermMonths,
    Guid lenderId)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        throw new DomainException("Loan product title is required.");
    }

    if (title.Length > 150)
    {
        throw new DomainException("Loan product title cannot exceed 150 characters.");
    }

    if (string.IsNullOrWhiteSpace(description))
    {
        throw new DomainException("Loan product description is required.");
    }

    if (minimumAmount.Amount <= 0)
    {
        throw new DomainException("Minimum loan amount must be greater than zero.");
    }

    if (maximumAmount.Amount <= 0)
    {
        throw new DomainException("Maximum loan amount must be greater than zero.");
    }

    if (minimumAmount.Currency != maximumAmount.Currency)
    {
        throw new DomainException("Minimum and maximum loan amounts must use the same currency.");
    }

    if (minimumAmount.Amount > maximumAmount.Amount)
    {
        throw new DomainException("Minimum loan amount cannot be greater than maximum loan amount.");
    }

    if (minimumTermMonths <= 0)
    {
        throw new DomainException("Minimum term must be greater than zero.");
    }

    if (maximumTermMonths <= 0)
    {
        throw new DomainException("Maximum term must be greater than zero.");
    }

    if (minimumTermMonths > maximumTermMonths)
    {
        throw new DomainException("Minimum term cannot be greater than maximum term.");
    }

    if (lenderId == Guid.Empty)
    {
        throw new DomainException("Lender id is required.");
    }

    return new LoanProduct(
        title.Trim(),
        description.Trim(),
        minimumAmount,
        maximumAmount,
        interestRate,
        minimumTermMonths,
        maximumTermMonths,
        lenderId);
}
```

**Notice how value objects simplify validation:**
- `InterestRate interestRate` — we don't need to validate the rate here because `InterestRate.Create()` already guarantees it's between 0 and 100.
- `Money minimumAmount` — we know it's non-negative and has a valid currency. We only need to check the business rule that min < max.
- This is the power of value objects: **validation composes**.

---

## 5. State Machines

### What is a State Machine in Domain Terms?

A state machine defines:
1. **States** — the possible statuses an entity can be in (represented by an enum).
2. **Transitions** — the allowed moves between states (represented by methods).
3. **Guard clauses** — conditions that must be true for a transition to happen (the `if` checks at the top of each method).

If you try an illegal transition, the entity throws a `DomainException`. This means **the entity itself enforces its lifecycle** — no external code can put it into an invalid state.

---

### 5.1 LoanProduct Lifecycle

```
Draft → PendingApproval → Approved → Published → Archived
                                                      ↑
                                   (can archive from any non-archived state)
```

**Status Enum:**
```csharp
public enum LoanProductStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Published = 4,
    Archived = 5
}
```

**Transition: Draft → PendingApproval**
```csharp
public void SubmitForApproval()
{
    if (Status != LoanProductStatus.Draft)
    {
        throw new DomainException("Only draft loan products can be submitted for approval.");
    }

    Status = LoanProductStatus.PendingApproval;
    MarkUpdated();
}
```
*Guard:* Must be in Draft. A published product can't go back to pending.

**Transition: PendingApproval → Approved**
```csharp
public void Approve()
{
    if (Status != LoanProductStatus.PendingApproval)
    {
        throw new DomainException("Only pending loan products can be approved.");
    }

    Status = LoanProductStatus.Approved;
    MarkUpdated();
}
```
*Guard:* Must be PendingApproval. You can't approve a draft (it hasn't been submitted yet).

**Transition: Approved → Published**
```csharp
public void Publish()
{
    if (Status != LoanProductStatus.Approved)
    {
        throw new DomainException("Only approved loan products can be published.");
    }

    Status = LoanProductStatus.Published;
    MarkUpdated();
}
```
*Guard:* Must be Approved. This ensures the approval workflow can't be bypassed.

**Transition: Any (non-archived) → Archived**
```csharp
public void Archive()
{
    if (Status == LoanProductStatus.Archived)
    {
        throw new DomainException("Loan product is already archived.");
    }

    Status = LoanProductStatus.Archived;
    MarkUpdated();
}
```
*Guard:* Only checks it's not already archived. A product can be archived from any state (draft, pending, approved, or published).

**Bonus: UpdateDetails() — Editing with State Guards**
```csharp
public void UpdateDetails(
    string title,
    string description,
    Money minimumAmount,
    Money maximumAmount,
    InterestRate interestRate,
    int minimumTermMonths,
    int maximumTermMonths)
{
    if (Status is LoanProductStatus.Published or LoanProductStatus.Archived)
    {
        throw new DomainException("Published or archived loan products cannot be edited.");
    }

    // Re-uses Create() validation by creating a temporary instance
    var updated = Create(
        title, description, minimumAmount, maximumAmount,
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
```
*Guard:* Can't edit published or archived products. Notice the clever trick: it calls `Create()` to validate the new values, then copies them over. This avoids duplicating validation logic.

---

### 5.2 LoanApplication Lifecycle

```
Draft → Submitted → UnderReview → Approved → Funded
                         ↓              ↓
                  DocumentsRequested  Rejected
                         ↓
                    (resubmit → UnderReview)

Draft → Withdrawn (borrower can withdraw a draft)
```

**Status Enum:**
```csharp
public enum LoanApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Funded = 6,
    Withdrawn = 7,
    DocumentsRequested = 8
}
```

**Two Factory Methods — Create() and CreateDraft():**

`Create()` — creates a submitted application (product already selected):
```csharp
public static LoanApplication Create(
    Guid borrowerId,
    Guid loanProductId,
    Money requestedAmount,
    int termMonths,
    string purpose)
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

    return new LoanApplication(borrowerId, loanProductId, requestedAmount, termMonths, purpose.Trim());
    // Status = Submitted, SubmittedAtUtc = DateTime.UtcNow
}
```

`CreateDraft()` — creates a draft without a product selected:
```csharp
public static LoanApplication CreateDraft(
    Guid borrowerId,
    decimal requestedAmount,
    int termMonths,
    string purpose)
{
    // Same validations minus loanProductId...
    return new LoanApplication(borrowerId, Money.Create(requestedAmount), termMonths, purpose.Trim());
    // Status = Draft, SubmittedAtUtc = null
}
```

**Transition: Draft → Draft (update parameters)**
```csharp
public void UpdateParameters(decimal amount, int termMonths, string purpose)
{
    if (Status != LoanApplicationStatus.Draft)
    {
        throw new InvalidOperationException(
            $"Cannot update parameters when application is in '{Status}' status. Only draft applications can be updated.");
    }

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

**Transition: Draft → Draft (select product)**
```csharp
public void SelectProduct(Guid loanProductId)
{
    if (Status != LoanApplicationStatus.Draft)
    {
        throw new InvalidOperationException(
            $"Cannot select a product when application is in '{Status}' status. Only draft applications can have a product selected.");
    }

    if (loanProductId == Guid.Empty)
        throw new DomainException("Loan product id is required.");

    LoanProductId = loanProductId;
    MarkUpdated();
}
```

**Transition: Draft → Submitted**
```csharp
public void Submit()
{
    if (Status != LoanApplicationStatus.Draft)
    {
        throw new InvalidOperationException(
            $"Cannot submit application when it is in '{Status}' status. Only draft applications can be submitted.");
    }

    if (LoanProductId is null || LoanProductId == Guid.Empty)
    {
        throw new InvalidOperationException(
            "Cannot submit application without a selected loan product.");
    }

    Status = LoanApplicationStatus.Submitted;
    SubmittedAtUtc = DateTime.UtcNow;
    MarkUpdated();
}
```
*Guard:* Must be Draft AND must have a product selected. Two conditions!

**Transition: Submitted → UnderReview**
```csharp
public void MarkUnderReview()
{
    if (Status != LoanApplicationStatus.Submitted)
    {
        throw new InvalidOperationException(
            $"Cannot move application to under review when it is in '{Status}' status. Only submitted applications can move under review.");
    }

    Status = LoanApplicationStatus.UnderReview;
    MarkUpdated();
}
```

**Transition: UnderReview → Approved**
```csharp
public void Approve(string reason, string reviewedBy)
{
    if (Status != LoanApplicationStatus.UnderReview)
    {
        throw new InvalidOperationException(
            $"Cannot approve application when it is in '{Status}' status. Only applications under review can be approved.");
    }

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
```
*Note:* Requires a reason and reviewer identity — creates an audit trail.

**Transition: UnderReview → Rejected**
```csharp
public void Reject(string reason, string reviewedBy)
{
    if (Status != LoanApplicationStatus.UnderReview)
    {
        throw new InvalidOperationException(
            $"Cannot reject application when it is in '{Status}' status. Only applications under review can be rejected.");
    }

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
```

**Transition: UnderReview → DocumentsRequested**
```csharp
public void RequestDocuments(string note, string requestedBy)
{
    if (Status != LoanApplicationStatus.UnderReview)
    {
        throw new InvalidOperationException(
            $"Cannot request documents when application is in '{Status}' status. Only applications under review can have documents requested.");
    }

    if (string.IsNullOrWhiteSpace(note))
        throw new DomainException("Document request note is required.");
    if (string.IsNullOrWhiteSpace(requestedBy))
        throw new DomainException("Requester identity is required.");

    Status = LoanApplicationStatus.DocumentsRequested;
    DocumentRequestNote = note;
    ReviewedBy = requestedBy;
    MarkUpdated();
}
```

**Transition: DocumentsRequested → UnderReview (resubmit)**
```csharp
public void ResubmitForReview()
{
    if (Status != LoanApplicationStatus.DocumentsRequested)
    {
        throw new InvalidOperationException(
            $"Cannot resubmit for review when application is in '{Status}' status. Only applications with documents requested can be resubmitted.");
    }

    Status = LoanApplicationStatus.UnderReview;
    MarkUpdated();
}
```
*This creates a loop:* UnderReview → DocumentsRequested → UnderReview → ... until approved or rejected.

**Transition: Approved → Funded**
```csharp
public void Fund()
{
    if (Status != LoanApplicationStatus.Approved)
    {
        throw new InvalidOperationException(
            $"Cannot fund application when it is in '{Status}' status. Only approved applications can be funded.");
    }

    Status = LoanApplicationStatus.Funded;
    MarkUpdated();
}
```

**Transition: Draft → Withdrawn**
```csharp
public void Withdraw()
{
    if (Status != LoanApplicationStatus.Draft)
    {
        throw new InvalidOperationException(
            $"Cannot withdraw application when it is in '{Status}' status. Only draft applications can be withdrawn.");
    }

    Status = LoanApplicationStatus.Withdrawn;
    MarkUpdated();
}
```
*Note:* Only drafts can be withdrawn. Once submitted, the application is in the system.

---

### 5.3 Installment Lifecycle

```
Pending → Paid (full payment)
Pending → PartiallyPaid (partial payment)
Pending → Late (overdue)
PartiallyPaid → Paid (remaining paid)
PartiallyPaid → Late (overdue)
Late → Paid (full payment including late fee)
Late → PartiallyPaid (partial payment)
Late → Missed (never paid, next installment due)
```

**Status Enum:**
```csharp
public enum InstallmentStatus
{
    Pending = 1,       // Payment not yet due or awaiting payment
    Paid = 2,          // Full payment received
    PartiallyPaid = 3, // Partial payment received, balance remaining
    Late = 4,          // Payment is overdue past the grace period
    Missed = 5         // Payment was not made and the next installment is now due
}
```

**Transition: Any (non-paid) → Paid via RecordFullPayment()**
```csharp
public void RecordFullPayment(DateTime paymentDate)
{
    if (Status == InstallmentStatus.Paid)
    {
        throw new DomainException("Installment is already fully paid.");
    }

    var totalOwed = TotalAmount + LateFeeAmount;
    PaidAmount = totalOwed;
    PaidDate = paymentDate;
    Status = InstallmentStatus.Paid;
    MarkUpdated();
}
```
*Guard:* Can't pay something that's already paid. Sets `PaidAmount` to the full amount owed (including any late fees).

**Transition: Any (non-paid) → PartiallyPaid or Paid via RecordPartialPayment()**
```csharp
public void RecordPartialPayment(decimal amount, DateTime paymentDate)
{
    if (amount <= 0)
    {
        throw new DomainException("Payment amount must be greater than zero.");
    }

    if (Status == InstallmentStatus.Paid)
    {
        throw new DomainException("Installment is already fully paid.");
    }

    var totalOwed = TotalAmount + LateFeeAmount;
    var newPaidAmount = PaidAmount + amount;

    if (newPaidAmount > totalOwed)
    {
        throw new DomainException(
            $"Payment of {amount:N2} would exceed the total owed of {totalOwed:N2}. " +
            $"Maximum additional payment allowed is {totalOwed - PaidAmount:N2}.");
    }

    PaidAmount = newPaidAmount;
    PaidDate = paymentDate;

    if (PaidAmount >= totalOwed)
    {
        Status = InstallmentStatus.Paid;
    }
    else
    {
        Status = InstallmentStatus.PartiallyPaid;
    }

    MarkUpdated();
}
```
*Key logic:* Accumulates payments. If the cumulative amount reaches the total owed, it transitions to Paid. Otherwise, PartiallyPaid. Prevents overpayment with a clear error message.

**Transition: Pending/PartiallyPaid → Late via MarkLate()**
```csharp
public void MarkLate(decimal lateFeePercentage)
{
    if (Status != InstallmentStatus.Pending && Status != InstallmentStatus.PartiallyPaid)
    {
        throw new DomainException(
            $"Cannot mark installment as late when status is '{Status}'. " +
            "Only Pending or PartiallyPaid installments can be marked late.");
    }

    Status = InstallmentStatus.Late;
    LateFeeAmount = decimal.Round((TotalAmount - PaidAmount) * lateFeePercentage, 2);
    MarkUpdated();
}
```
*Guard:* Only Pending or PartiallyPaid can become Late. Calculates the late fee as a percentage of the remaining unpaid balance.

**Transition: Late → Missed via MarkMissed()**
```csharp
public void MarkMissed()
{
    if (Status != InstallmentStatus.Late)
    {
        throw new DomainException(
            $"Cannot mark installment as missed when status is '{Status}'. " +
            "Only Late installments can be marked as missed.");
    }

    Status = InstallmentStatus.Missed;
    MarkUpdated();
}
```
*Guard:* Only Late installments can be marked Missed. This is a strict progression: Pending → Late → Missed.

**Utility Methods:**
```csharp
public void MarkReminderSent()
{
    ReminderSent = true;
    MarkUpdated();
}

public void MarkLateNoticeSent()
{
    LateNoticeSent = true;
    MarkUpdated();
}
```
These track communication state — whether the borrower has been notified.

---

### 5.4 Lender Lifecycle

```
PendingVerification → Verified → Suspended → Archived
                          ↑           ↓
                          └───────────┘ (can re-verify? No — suspend is one-way from verified)
                          
PendingVerification → Suspended (can suspend from any non-archived state)
PendingVerification → Archived (can archive from any non-archived state)
Verified → Suspended
Verified → Archived
Suspended → Archived
```

**Status Enum:**
```csharp
public enum LenderStatus
{
    Draft = 1,
    PendingVerification = 2,
    Verified = 3,
    Suspended = 4,
    Archived = 5
}
```

**Transition: PendingVerification → Verified**
```csharp
public void Verify()
{
    if (Status != LenderStatus.PendingVerification)
    {
        throw new DomainException("Only pending lenders can be verified.");
    }

    Status = LenderStatus.Verified;
    MarkUpdated();
}
```
*Guard:* Strict — only PendingVerification can become Verified.

**Transition: Any (non-archived) → Suspended**
```csharp
public void Suspend()
{
    if (Status == LenderStatus.Archived)
    {
        throw new DomainException("Archived lenders cannot be suspended.");
    }

    Status = LenderStatus.Suspended;
    MarkUpdated();
}
```
*Guard:* Can suspend from any state except Archived. This is intentional — an admin might need to suspend a lender at any point.

**Transition: Any (non-archived) → Archived**
```csharp
public void Archive()
{
    if (Status == LenderStatus.Archived)
    {
        throw new DomainException("Lender is already archived.");
    }

    Status = LenderStatus.Archived;
    MarkUpdated();
}
```
*Guard:* Only prevents double-archiving. Archive is a terminal state.

**Fund Management Methods:**
```csharp
public void DeductFunds(decimal amount)
{
    if (amount <= 0)
    {
        throw new DomainException("Deduction amount must be greater than zero.");
    }

    if (amount > AvailableFunds)
    {
        throw new DomainException("Insufficient funds. The deduction amount exceeds available funds.");
    }

    AvailableFunds -= amount;
    MarkUpdated();
}

public void TopUpFunds(decimal amount)
{
    if (amount <= 0)
    {
        throw new DomainException("Top-up amount must be greater than zero.");
    }

    AvailableFunds += amount;
    MarkUpdated();
}
```
These aren't state transitions but they follow the same pattern: validate, mutate, mark updated.

---

### 5.5 Borrower Lifecycle

```
PendingVerification → Verified → Suspended → Archived
                          ↑           ↓
                          └───────────┘
                          
(Same pattern as Lender — suspend from any non-archived, archive from any non-archived)
```

**Status Enum:**
```csharp
public enum BorrowerStatus
{
    Draft = 1,
    PendingVerification = 2,
    Verified = 3,
    Suspended = 4,
    Archived = 5
}
```

**Transition: PendingVerification → Verified**
```csharp
public void Verify()
{
    if (Status != BorrowerStatus.PendingVerification)
    {
        throw new DomainException("Only pending borrowers can be verified.");
    }

    Status = BorrowerStatus.Verified;
    MarkUpdated();
}
```

**Transition: Any (non-archived) → Suspended**
```csharp
public void Suspend()
{
    if (Status == BorrowerStatus.Archived)
    {
        throw new DomainException("Archived borrowers cannot be suspended.");
    }

    Status = BorrowerStatus.Suspended;
    MarkUpdated();
}
```

**Transition: Any (non-archived) → Archived**
```csharp
public void Archive()
{
    if (Status == BorrowerStatus.Archived)
    {
        throw new DomainException("Borrower is already archived.");
    }

    Status = BorrowerStatus.Archived;
    MarkUpdated();
}
```

The Borrower and Lender lifecycles are intentionally identical — they represent the same business concept of "user verification and account management."

---

## 6. Domain Services

### When to Use a Domain Service vs. an Entity Method

| Use an Entity Method when... | Use a Domain Service when... |
|---|---|
| The operation naturally belongs to one entity | The operation spans multiple entities |
| The entity has all the data it needs | The operation needs data from multiple aggregates |
| Example: `lender.DeductFunds(amount)` | Example: Processing a payment against a schedule + installment |

### PaymentProcessor — A Domain Service

The `PaymentProcessor` coordinates between `RepaymentSchedule` and `Installment`. It can't live on either entity alone because it needs to:
1. Find the next pending installment from the schedule
2. Determine if the payment is full or partial
3. Call the appropriate method on the installment
4. Update the schedule's performance rating

**Interface:**
```csharp
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

public interface IPaymentProcessor
{
    /// Records a payment against the next pending installment in the schedule.
    void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate);

    /// Records a bulk payment that pays off multiple installments sequentially.
    /// Returns the number of installments fully paid.
    int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate);
}
```

**Implementation:**
```csharp
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

public sealed class PaymentProcessor : IPaymentProcessor
{
    public void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        var nextInstallment = schedule.GetNextPendingInstallment();

        if (nextInstallment is null)
        {
            throw new DomainException("No pending installments found. All payments are complete.");
        }

        var totalOwed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount - nextInstallment.PaidAmount;

        if (amount > totalOwed)
        {
            throw new DomainException(
                $"Payment of {amount:N2} exceeds the remaining balance of {totalOwed:N2} " +
                $"on installment #{nextInstallment.InstallmentNumber}.");
        }

        if (amount >= totalOwed)
        {
            nextInstallment.RecordFullPayment(paymentDate);
        }
        else
        {
            nextInstallment.RecordPartialPayment(amount, paymentDate);
        }

        schedule.UpdatePerformance();
    }

    public int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate)
    {
        if (totalAmount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        var remaining = totalAmount;
        var installmentsPaid = 0;

        while (remaining > 0)
        {
            var nextInstallment = schedule.GetNextPendingInstallment();
            if (nextInstallment is null)
            {
                break; // All installments paid off
            }

            var owed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount - nextInstallment.PaidAmount;

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

**Key design decisions:**
- The service is **stateless** — it has no fields, no constructor dependencies. Pure logic.
- It **delegates to entity methods** (`RecordFullPayment`, `RecordPartialPayment`) rather than directly mutating state.
- It **enforces sequential payment order** — you always pay the next pending installment first.
- `RecordBulkPayment` handles the case where a borrower pays multiple months at once.

---

## 7. Domain Events (Brief)

### The Pattern

Domain events represent **something that happened** in the domain that other parts of the system might care about. In this project, we use MediatR's `INotification` pattern.

**When to raise domain events:**
- A loan application is approved → notify the borrower via email
- A payment is recorded → update analytics dashboards
- A lender is verified → send a welcome email

**Structure:**
```csharp
// Example pattern (Events folder is currently empty — ready for you to add)
using MediatR;

public record LoanApplicationApprovedEvent(Guid LoanApplicationId, Guid BorrowerId) : INotification;
```

**How it works:**
1. The entity (or application layer) publishes the event.
2. MediatR dispatches it to all registered handlers.
3. Handlers perform side effects (send emails, update read models, etc.).

**Key rule:** Domain events are for **side effects**, not for core business logic. The entity's state machine handles the business rules; events handle the consequences.

---

## 8. Key Rules for the Domain Layer

### Rule 1: No Framework Dependencies

Look at the `.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

The Domain project has **no references** to Entity Framework, ASP.NET, or any infrastructure package (aside from Identity Stores for the user entity). This is intentional — the domain should be testable in isolation without spinning up a database or web server.

### Rule 2: Entities Are Always in a Valid State

- Factory methods validate all inputs before construction.
- State transition methods check preconditions before mutating.
- If you have a reference to an entity, you can trust it's valid.

### Rule 3: All Mutations Go Through Methods (No Public Setters)

Every property uses `private set`:
```csharp
public string CompanyName { get; private set; }
public LenderStatus Status { get; private set; }
```

The only way to change state is through a named method like `Verify()`, `Suspend()`, or `DeductFunds()`. This makes the code self-documenting — you can see every possible mutation by looking at the entity's public methods.

### Rule 4: Exceptions for Invalid Operations (Not Null Returns)

When something is wrong, we throw:
```csharp
// ✅ Clear, immediate feedback
throw new DomainException("Only pending lenders can be verified.");

// ❌ Never do this — caller might forget to check
return null;  // "it didn't work, figure out why"
```

Exceptions make invalid operations **impossible to ignore**. The Application layer catches `DomainException` and returns appropriate error responses.

### Rule 5: Sealed Classes

All entities are `sealed`:
```csharp
public sealed class Lender : AuditableEntity { }
```

This prevents inheritance hierarchies that could break invariants. If you need shared behavior, use composition or base classes (like `AuditableEntity`).

---

## 9. How to Create a New Entity

Follow this step-by-step template when adding a new entity to the domain.

### Step 1: Define the Status Enum (if the entity has a lifecycle)

```csharp
// src/LoanSuperMarket.Domain/Enums/YourEntityStatus.cs
namespace LoanSuperMarket.Domain.Enums;

public enum YourEntityStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}
```

### Step 2: Create the Entity Class

```csharp
// src/LoanSuperMarket.Domain/Entities/YourEntity.cs
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class YourEntity : AuditableEntity
{
    // 1. Parameterless private constructor for EF Core
    private YourEntity()
    {
        Name = string.Empty;
    }

    // 2. Real private constructor
    private YourEntity(string name, Guid ownerId)
    {
        Name = name;
        OwnerId = ownerId;
        Status = YourEntityStatus.Draft;  // Always set initial state
    }

    // 3. Properties with private setters
    public string Name { get; private set; }
    public Guid OwnerId { get; private set; }
    public YourEntityStatus Status { get; private set; }

    // 4. Static factory method with validation
    public static YourEntity Create(string name, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        if (name.Length > 200)
            throw new DomainException("Name cannot exceed 200 characters.");

        if (ownerId == Guid.Empty)
            throw new DomainException("Owner id is required.");

        return new YourEntity(name.Trim(), ownerId);
    }

    // 5. State transition methods with guard clauses
    public void Activate()
    {
        if (Status != YourEntityStatus.Draft)
            throw new DomainException("Only draft entities can be activated.");

        Status = YourEntityStatus.Active;
        MarkUpdated();
    }

    public void Complete()
    {
        if (Status != YourEntityStatus.Active)
            throw new DomainException("Only active entities can be completed.");

        Status = YourEntityStatus.Completed;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status == YourEntityStatus.Cancelled)
            throw new DomainException("Entity is already cancelled.");

        Status = YourEntityStatus.Cancelled;
        MarkUpdated();
    }
}
```

### Step 3: Create Value Objects (if needed)

If your entity has a concept that:
- Has multiple components (like amount + currency)
- Has validation rules
- Appears in multiple places

Then create a value object following the `Money` or `InterestRate` pattern.

### Step 4: Checklist

Before you're done, verify:

- [ ] Entity is `sealed`
- [ ] Entity inherits from `AuditableEntity`
- [ ] All constructors are `private`
- [ ] All property setters are `private set`
- [ ] There's a parameterless constructor for EF Core
- [ ] There's a static `Create()` factory method
- [ ] `Create()` validates ALL inputs before construction
- [ ] Initial state is set in the constructor (not left as default)
- [ ] Every state transition method has a guard clause
- [ ] Every mutation calls `MarkUpdated()` at the end
- [ ] Exception messages are human-readable and specific
- [ ] Strings are trimmed, emails are lowercased
- [ ] No dependencies on infrastructure (no `DbContext`, no `HttpClient`, etc.)

### Step 5: Write Tests

Create a test class that verifies:
1. `Create()` succeeds with valid inputs
2. `Create()` throws for each invalid input
3. Each state transition succeeds from the correct state
4. Each state transition throws from incorrect states

---

## Summary

The Domain layer is the heart of this application. It encodes every business rule in code that is:
- **Self-validating** — entities refuse invalid data
- **Self-protecting** — state machines prevent illegal transitions
- **Self-documenting** — method names match business operations
- **Framework-independent** — testable without infrastructure
- **Auditable** — every change is timestamped automatically

When in doubt, ask: "Would a product manager understand this method name?" If yes, you're doing DDD right.
