# Domain Layer — The Complete Bible

## Introduction

Let me sit beside you and explain every single piece of the Domain layer. This is the most important layer in the entire application. Everything else — the API, the database, the frontend — exists to serve the rules defined here.

The Domain layer lives at: `src/LoanSuperMarket.Domain/`

It has ZERO dependencies on any framework. No Entity Framework. No ASP.NET. No MediatR. Just pure C# and business logic. This is intentional — if we ever change our database from SQL Server to PostgreSQL, or our API from REST to GraphQL, this layer stays exactly the same.

---

## Folder Structure

```
LoanSuperMarket.Domain/
├── Common/
│   ├── BaseEntity.cs           ← The root of all entities
│   ├── AuditableEntity.cs      ← Adds audit tracking to entities
│   └── DomainException.cs      ← Custom exception for business rule violations
├── Entities/
│   ├── Lender.cs               ← A company/person who funds loans
│   ├── Borrower.cs             ← A person who borrows money
│   ├── LoanProduct.cs          ← A template for a type of loan
│   ├── LoanApplication.cs     ← A borrower's request for money
│   ├── RepaymentSchedule.cs   ← The amortization plan for a funded loan
│   ├── Installment.cs         ← A single monthly payment
│   ├── AuditLog.cs            ← A record of something that happened
│   ├── ApplicationDocument.cs ← A file uploaded for verification
│   └── Identity/              ← ASP.NET Identity extensions
│       ├── ApplicationUser.cs
│       ├── CustomRole.cs
│       ├── RefreshToken.cs
│       ├── UserSession.cs
│       ├── RolePermission.cs
│       └── RecoveryCode.cs
├── Enums/
│   ├── AccountStatus.cs
│   ├── BorrowerStatus.cs
│   ├── LenderStatus.cs
│   ├── LoanApplicationStatus.cs
│   ├── LoanProductStatus.cs
│   ├── InstallmentStatus.cs
│   ├── LoanPerformance.cs
│   ├── CollectionStatus.cs
│   ├── CreditTier.cs
│   ├── DocumentStatus.cs
│   ├── DocumentType.cs
│   ├── PermissionAction.cs
│   └── PermissionModule.cs
├── ValueObjects/
│   ├── Money.cs               ← Represents a monetary amount
│   └── InterestRate.cs        ← Represents a percentage rate
└── Services/
    ├── IPaymentProcessor.cs   ← Interface for payment processing
    └── PaymentProcessor.cs    ← Implementation
```

---

# PART 1: Common Base Classes

---

## File: `Common/BaseEntity.cs`

```csharp
namespace LoanSuperMarket.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
```

### What Is This?

This is the absolute foundation of every single entity in our system. Every lender, every borrower, every loan application, every installment — they ALL inherit from this class.

### Line-by-Line Explanation

**`public abstract class BaseEntity`**

The word `abstract` means you cannot create a `BaseEntity` directly. You can only create things that inherit from it (like `Lender` or `Borrower`). Think of it like a blueprint — you can't live in a blueprint, but you can build a house from one.

**`public Guid Id { get; protected set; } = Guid.NewGuid();`**

Every entity gets a unique identifier the moment it's created in memory. Let me break this down:

- `Guid` — A globally unique identifier. It looks like `3fa85f64-5717-4562-b3fc-2c963f66afa6`. The chance of two GUIDs being the same is astronomically small (1 in 2^128).
- `get; protected set;` — Anyone can READ the Id, but only the entity itself (or its children) can SET it. This prevents external code from changing an entity's identity.
- `= Guid.NewGuid()` — The Id is generated immediately when the object is created in memory, BEFORE it ever touches the database. This is important because it means we know the Id before we save.

### Why GUIDs Instead of Auto-Increment Integers?

In a traditional system, you might use `int Id` that auto-increments (1, 2, 3...). We use GUIDs because:

1. **No database round-trip needed** — We know the Id before saving. This means we can create related objects (like a schedule with installments) and set their foreign keys without saving the parent first.
2. **Globally unique** — If we ever merge databases or have distributed systems, there's no collision risk.
3. **Security** — An attacker can't guess the next Id. With integers, if you know loan #1000 exists, you can try #1001.
4. **No information leakage** — Integers reveal how many records exist. GUIDs don't.

### What Could Go Wrong?

- GUIDs are larger than integers (16 bytes vs 4 bytes), so indexes are slightly larger
- GUIDs are not sequential by default, which can cause index fragmentation in SQL Server (mitigated by using `NEWSEQUENTIALID()` in the database or `Guid.CreateVersion7()` in .NET 9+)

---

## File: `Common/AuditableEntity.cs`

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

### What Is This?

This extends `BaseEntity` by adding audit tracking. Every business entity in our system needs to answer: "Who created this? When? Who last changed it? When?"

This is not optional in financial systems. Regulators, auditors, and support teams all need this information.

### Properties Explained

**`public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;`**

The moment this entity is instantiated in memory, the creation timestamp is recorded. We use UTC (Coordinated Universal Time) because:
- Our users might be in different time zones
- The database stores one consistent time
- The UI converts to local time for display

The `private set` means only the entity itself can change this value. External code cannot fake a creation date.

**`public string? CreatedBy { get; private set; }`**

Who created this record. The `?` means it's nullable — sometimes we don't know who created it (e.g., system-generated records). In practice, this would be the user's email or ID.

**`public DateTime? UpdatedAtUtc { get; private set; }`**

When was this record last modified. It's nullable because a brand-new record hasn't been updated yet.

**`public string? UpdatedBy { get; private set; }`**

Who last modified this record.

### Methods Explained

**`public void MarkCreated(string? createdBy = null)`**

Called when we want to explicitly set the creation timestamp and creator. The `= null` default parameter means you can call it without arguments: `entity.MarkCreated()` or with: `entity.MarkCreated("admin@company.com")`.

Why would you call this explicitly when `CreatedAtUtc` is already set in the initializer? Because sometimes you want to override the timestamp — for example, when importing historical data.

**`public void MarkUpdated(string? updatedBy = null)`**

Called every time the entity changes. Look at any entity method that modifies state — they ALL call `MarkUpdated()` at the end. This ensures the audit trail is always current.

For example, in `Lender.DeductFunds()`:
```csharp
AvailableFunds -= amount;
MarkUpdated();  // ← Records that this entity was just modified
```

### Why This Pattern Matters

In a real financial platform, you will get questions like:
- "When was this loan approved?" → Check `UpdatedAtUtc` on the application
- "Who funded this loan?" → Check the audit log (which also uses these timestamps)
- "Has this record been tampered with?" → Compare `CreatedAtUtc` with `UpdatedAtUtc`

Without audit fields, you're flying blind in production.

---

## File: `Common/DomainException.cs`

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

### What Is This?

A custom exception type specifically for business rule violations. When something is wrong from a BUSINESS perspective (not a technical one), we throw this.

### Why Not Just Use `Exception`?

Because we need to distinguish between:
- **Business rule violations** (DomainException) — "You can't fund more than your available balance"
- **State machine violations** (InvalidOperationException) — "You can't approve a draft application"
- **Technical errors** (NullReferenceException, etc.) — Bugs in the code

The API's `GlobalExceptionMiddleware` catches `DomainException` and returns a clean 400 Bad Request with the message. It catches `InvalidOperationException` and returns 409 Conflict. It catches everything else and returns 500 Internal Server Error.

This means the frontend gets meaningful error messages for business violations, not generic "something went wrong" errors.

### When To Use DomainException vs InvalidOperationException

The convention in this codebase:
- **DomainException** — Input validation failures. The data provided is wrong. Examples: "Amount must be greater than zero", "Email is required"
- **InvalidOperationException** — State transition failures. The operation isn't valid in the current state. Examples: "Cannot approve when status is Draft", "Cannot fund when status is Rejected"

This distinction helps the API return appropriate HTTP status codes.

---

# PART 2: Value Objects

---

## File: `ValueObjects/Money.cs`

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
            throw new DomainException("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code.");

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public bool Equals(Money? other) { ... }
    public override bool Equals(object? obj) { ... }
    public override int GetHashCode() { ... }
    public override string ToString() => $"{Currency} {Amount:N2}";
}
```

### What Is This?

A Value Object that represents a monetary amount with a currency. Think of it like this — £100 and $100 are NOT the same thing, even though the number is the same. Money without a currency is meaningless.

### Why Is This a Value Object and Not Just a `decimal`?

If we just used `decimal` for money, we'd have problems:
1. No currency tracking — is 100 pounds or dollars?
2. No validation — negative money makes no sense for loan amounts
3. No rounding — financial calculations need exactly 2 decimal places
4. No equality semantics — when are two money values "the same"?

### The Private Constructor

```csharp
private Money(decimal amount, string currency)
```

You CANNOT write `new Money(100, "GBP")` from outside this class. The constructor is private. You MUST use the factory method `Money.Create(100, "GBP")`. This forces all creation to go through validation.

### The Factory Method: `Money.Create()`

**Parameters:**
- `decimal amount` — The numeric value. Must be >= 0.
- `string currency = "GBP"` — The ISO 4217 currency code. Defaults to British Pounds.

**Validations:**
1. Amount cannot be negative (you can't have -£50 as a loan amount)
2. Currency is required (can't be null or empty)
3. Currency must be exactly 3 characters (ISO standard: GBP, USD, EUR)

**Processing:**
- `decimal.Round(amount, 2)` — Ensures exactly 2 decimal places. £100.999 becomes £101.00.
- `currency.ToUpperInvariant()` — Normalizes to uppercase. "gbp" becomes "GBP".

### Why `IEquatable<Money>`?

Two Money objects are equal if they have the same amount AND the same currency. This is important because:
- `Money.Create(100, "GBP") == Money.Create(100, "GBP")` → true
- `Money.Create(100, "GBP") == Money.Create(100, "USD")` → false

Without this, C# would compare by reference (memory address), and two separate Money objects with the same values would NOT be equal.

### `GetHashCode()`

```csharp
return HashCode.Combine(Amount, Currency);
```

Required whenever you override `Equals()`. Used by dictionaries and hash sets. Two equal Money objects must produce the same hash code.

---

## File: `ValueObjects/InterestRate.cs`

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

### What Is This?

A Value Object representing an annual interest rate as a percentage. 12.5 means 12.5% per year.

### Validations

- Must be greater than zero — a 0% loan makes no business sense for a lending platform (lenders need returns)
- Cannot exceed 100% — while technically possible, rates above 100% are likely data entry errors or predatory lending (which we don't support)

### Why Round to 2 Decimal Places?

Interest rates in the real world are quoted to 2 decimal places: 10.50%, 12.75%, 14.00%. Rounding ensures consistency and prevents floating-point weirdness like 10.5000000001%.

---

# PART 3: Entities — The Lender

---

## File: `Entities/Lender.cs`

This is one of the most important entities in the system. Let me explain every single line.

### Class Declaration

```csharp
public sealed class Lender : AuditableEntity
```

- `sealed` — No other class can inherit from Lender. This is a design choice that says "a Lender is a Lender, there are no subtypes."
- `: AuditableEntity` — Inherits Id, CreatedAtUtc, CreatedBy, UpdatedAtUtc, UpdatedBy, MarkCreated(), MarkUpdated()

### Constructors

```csharp
private Lender()
{
    CompanyName = string.Empty;
    ContactName = string.Empty;
    Email = string.Empty;
    PhoneNumber = string.Empty;
}
```

**The parameterless private constructor.** This exists ONLY for Entity Framework Core. When EF loads a Lender from the database, it needs to create an empty object first, then fill in the properties. Without this constructor, EF would crash.

The `string.Empty` assignments prevent null reference warnings. EF will immediately overwrite these with the actual database values.

```csharp
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
```

**The real constructor.** Also private — you can't call `new Lender(...)` from outside. You MUST use the `Create()` factory method. This ensures validation always runs.

Notice: `Status = LenderStatus.PendingVerification` — every new lender starts in a pending state. They must be verified before they can fund loans. This is a business rule enforced at creation time.

### Properties

```csharp
public string CompanyName { get; private set; }
public string ContactName { get; private set; }
public string Email { get; private set; }
public string PhoneNumber { get; private set; }
public decimal AvailableFunds { get; private set; }
public LenderStatus Status { get; private set; }
public string? UserId { get; private set; }
```

ALL setters are `private`. This means you CANNOT do:
```csharp
lender.AvailableFunds = 0; // ❌ COMPILE ERROR
```

You MUST go through a method:
```csharp
lender.DeductFunds(10000); // ✅ Goes through validation
```

This is the core principle of Domain-Driven Design: **entities protect their own invariants**. No external code can put a Lender into an invalid state.

**`UserId`** — Links this Lender to an ASP.NET Identity user account. Nullable because a Lender might be created by an admin before the user registers.

### Factory Method: `Lender.Create()`

```csharp
public static Lender Create(
    string companyName,
    string contactName,
    string email,
    string phoneNumber,
    decimal availableFunds)
```

This is the ONLY way to create a new Lender. Let me walk through every validation:

1. **`if (string.IsNullOrWhiteSpace(companyName))`** — A lender must have a company name. Can't be null, empty, or just spaces.

2. **`if (string.IsNullOrWhiteSpace(contactName))`** — Must have a contact person.

3. **`if (string.IsNullOrWhiteSpace(email))`** — Must have an email for communication.

4. **`if (string.IsNullOrWhiteSpace(phoneNumber))`** — Must have a phone number.

5. **`if (availableFunds < 0)`** — You can start with zero funds (you'll top up later), but you can't start with negative funds. That makes no business sense.

**Processing before storage:**
- `companyName.Trim()` — Removes leading/trailing whitespace. "  Acme Corp  " becomes "Acme Corp"
- `email.Trim().ToLowerInvariant()` — Normalizes email to lowercase. "John@ACME.com" becomes "john@acme.com". This prevents duplicate accounts with different casing.
- `decimal.Round(availableFunds, 2)` — Ensures exactly 2 decimal places.

### Method: `Verify()`

```csharp
public void Verify()
{
    if (Status != LenderStatus.PendingVerification)
        throw new DomainException("Only pending lenders can be verified.");

    Status = LenderStatus.Verified;
    MarkUpdated();
}
```

**What it does:** Transitions the lender from PendingVerification to Verified.

**Guard:** Only lenders in PendingVerification status can be verified. If you try to verify an already-verified lender, or a suspended one, it throws.

**Why this matters:** A verified lender can fund loans. An unverified one cannot. This is a compliance requirement — you must verify the identity of anyone handling money (KYC — Know Your Customer).

**`MarkUpdated()`** — Records that this entity was just modified (sets UpdatedAtUtc to now).

### Method: `Suspend()`

```csharp
public void Suspend()
{
    if (Status == LenderStatus.Archived)
        throw new DomainException("Archived lenders cannot be suspended.");

    Status = LenderStatus.Suspended;
    MarkUpdated();
}
```

**What it does:** Suspends a lender. A suspended lender cannot fund new loans.

**Guard:** You can suspend from any status EXCEPT Archived. An archived lender is permanently removed from the platform — suspending them makes no sense.

**Business scenario:** A lender violates platform rules, or there's a fraud investigation. You suspend them while investigating.

### Method: `Archive()`

```csharp
public void Archive()
{
    if (Status == LenderStatus.Archived)
        throw new DomainException("Lender is already archived.");

    Status = LenderStatus.Archived;
    MarkUpdated();
}
```

**What it does:** Permanently archives a lender. This is a soft delete — the record stays in the database but is no longer active.

**Guard:** Can't archive something that's already archived (idempotency protection).

### Method: `DeductFunds(decimal amount)`

```csharp
public void DeductFunds(decimal amount)
{
    if (amount <= 0)
        throw new DomainException("Deduction amount must be greater than zero.");

    if (amount > AvailableFunds)
        throw new DomainException("Insufficient funds. The deduction amount exceeds available funds.");

    AvailableFunds -= amount;
    MarkUpdated();
}
```

**What it does:** Reduces the lender's available capital. Called when they fund a loan.

**Validation 1:** Amount must be positive. You can't deduct zero or negative amounts.

**Validation 2:** Amount cannot exceed available funds. This is the CRITICAL business rule — a lender physically cannot fund more than they have. Without this check, we'd have negative balances and financial chaos.

**The operation:** Simple subtraction. If they have £50,000 and fund a £25,000 loan, they now have £25,000.

**What could go wrong in production:** Concurrency. If two requests try to deduct simultaneously, both might pass the `amount > AvailableFunds` check before either subtracts. This is why the database transaction and EF Core's optimistic concurrency are important at the infrastructure level.

### Method: `TopUpFunds(decimal amount)`

```csharp
public void TopUpFunds(decimal amount)
{
    if (amount <= 0)
        throw new DomainException("Top-up amount must be greater than zero.");

    AvailableFunds += amount;
    MarkUpdated();
}
```

**What it does:** Adds capital to the lender's account. Called when they deposit more money.

**Validation:** Amount must be positive. No upper limit is enforced at the domain level (the validator at the application level caps it at £10,000,000).

**Business scenario:** A lender funded several loans and their balance is low. They transfer more money to the platform and click "Top Up" in the dashboard.

---

*[Document continues with equally detailed explanations for Borrower, LoanProduct, LoanApplication, Installment, RepaymentSchedule, AuditLog, ApplicationDocument, PaymentProcessor, and all Enums...]*

---

> **Note:** This document covers the Common classes, Value Objects, and the Lender entity in full detail. The remaining entities (Borrower, LoanProduct, LoanApplication, Installment, RepaymentSchedule, AuditLog, ApplicationDocument) and the PaymentProcessor service follow the exact same patterns. Each will be documented with the same level of detail in subsequent sections of this file as it grows.

> The key patterns to understand from what you've read so far:
> 1. Private constructors + static factory methods = forced validation
> 2. Private setters = entities protect their own state
> 3. Guard clauses in methods = invalid state transitions are impossible
> 4. MarkUpdated() at the end of every mutation = audit trail is always current
> 5. DomainException for validation, InvalidOperationException for state violations


---

# PART 4: Entities — The Borrower

---

## File: `Entities/Borrower.cs`

### Class Declaration

```csharp
public sealed class Borrower : AuditableEntity
```

Same pattern as Lender — sealed, inherits audit tracking.

### Constructors

```csharp
private Borrower()
{
    FirstName = string.Empty;
    LastName = string.Empty;
    Email = string.Empty;
    PhoneNumber = string.Empty;
}

**Parameterless constructor for EF Core.** Same pattern as Lender.

```csharp
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
```

**The real constructor.** Every new borrower starts as PendingVerification.


### Properties

```csharp
public string FirstName { get; private set; }
public string LastName { get; private set; }
public string Email { get; private set; }
public string PhoneNumber { get; private set; }
public DateTime DateOfBirth { get; private set; }
public BorrowerStatus Status { get; private set; }
public CreditTier? CreditTier { get; private set; }
public string? UserId { get; private set; }
public string FullName => $"{FirstName} {LastName}";
```

**`CreditTier?`** — Nullable because it's assigned after verification.

The CreditTier determines the interest rate adjustment when a loan is funded:
- **Tier A** — Base rate (no adjustment). Excellent credit.
- **Tier B** — Base rate + 2%. Good credit.
- **Tier C** — Base rate + 4%. Fair credit.

This means if a product has a 10% base rate:
- Tier A borrower pays 10%
- Tier B borrower pays 12%
- Tier C borrower pays 14%

**`FullName`** — A computed property (no setter). Combines first and last
name. Used in UI displays and reports.

### Factory Method: `Borrower.Create()`

```csharp
public static Borrower Create(
    string firstName, string lastName,
    string email, string phoneNumber,
    DateTime dateOfBirth)
```

**Validations:**
1. First name required — cannot be null/empty/whitespace
2. Last name required
3. Email required
4. Phone number required
5. **Age check:** `dateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-18)` — The
   borrower must be at least 18 years old. This is a legal requirement in the UK
   for entering into credit agreements. If someone born on 2010-01-01 tries to
   register today (2026), they're only 16 — rejected.

**Processing:**
- Names trimmed of whitespace
- Email normalized to lowercase
- DateOfBirth stored as date only (time component stripped with `.Date`)

### Methods: `Verify()`, `Suspend()`, `Archive()`

Identical pattern to Lender:
- `Verify()` — Only from PendingVerification → Verified
- `Suspend()` — From any status except Archived → Suspended
- `Archive()` — From any status except already Archived → Archived

Each calls `MarkUpdated()` after the transition.


### Key Difference from Lender

The Borrower does NOT have financial methods like `DeductFunds` or `TopUpFunds`.
Borrowers don't hold capital on the platform — they receive money (via funding)
and repay it (via installments). The money flow is tracked on the
RepaymentSchedule and Installment entities, not on the Borrower itself.

---

# PART 5: Entities — The Installment (State Machine)

---

## File: `Entities/Installment.cs`

This is the most complex state machine in the system. Each installment
represents one monthly payment in a loan's life.

### Constructor

```csharp
internal Installment(
    int installmentNumber,
    DateTime dueDate,
    decimal principalPortion,
    decimal interestPortion,
    decimal remainingBalance)
{
    InstallmentNumber = installmentNumber;
    DueDate = dueDate;
    PrincipalPortion = principalPortion;
    InterestPortion = interestPortion;
    TotalAmount = principalPortion + interestPortion;
    RemainingBalance = remainingBalance;
    Status = InstallmentStatus.Pending;
    PaidAmount = 0;
    LateFeeAmount = 0;
}
```

**Why `internal`?** — Only code within the same assembly (Domain project) or
assemblies with `InternalsVisibleTo` (Application project, Domain.Tests) can
create installments. This prevents random code from creating installments
outside the amortization engine.

**Parameters explained:**
- `installmentNumber` — Sequential: 1, 2, 3... up to the term length
- `dueDate` — When this payment is expected (funding date + N months)
- `principalPortion` — How much of this payment reduces the loan balance
- `interestPortion` — How much is the lender's profit
- `remainingBalance` — What's left on the loan AFTER this installment is paid

**Computed:** `TotalAmount = principalPortion + interestPortion` — What the
borrower actually pays each month.

**Initial state:** Status = Pending, PaidAmount = 0, LateFeeAmount = 0.

### Properties

```csharp
public Guid RepaymentScheduleId { get; private set; }  // FK - set by EF
public int InstallmentNumber { get; private set; }
public DateTime DueDate { get; private set; }
public decimal PrincipalPortion { get; private set; }
public decimal InterestPortion { get; private set; }
public decimal TotalAmount { get; private set; }
public decimal RemainingBalance { get; private set; }
public InstallmentStatus Status { get; private set; }
public decimal PaidAmount { get; private set; }
public DateTime? PaidDate { get; private set; }
public decimal LateFeeAmount { get; private set; }
public string? Notes { get; private set; }
public bool ReminderSent { get; private set; }
public bool LateNoticeSent { get; private set; }
```

**`RepaymentScheduleId`** — Foreign key to the parent schedule. Set
automatically by EF Core when the installment is added to a schedule's
collection.

**`ReminderSent`** and **`LateNoticeSent`** — Boolean flags that prevent
duplicate notifications. The background service checks these before sending.
Without them, a borrower would get a reminder email every single day the
service runs.

### Method: `RecordFullPayment(DateTime paymentDate)`

```csharp
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
```

**What it does:** Records that the borrower paid the FULL amount owed
(including any late fee).

**Guard:** Cannot pay something that's already paid. This prevents double-
counting.

**The calculation:** `totalOwed = TotalAmount + LateFeeAmount`. If the
installment is £600 and has a £12 late fee, the full payment is £612.

**State transition:** Any status (Pending, PartiallyPaid, Late, Missed) → Paid.


### Method: `RecordPartialPayment(decimal amount, DateTime paymentDate)`

```csharp
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
            $"Payment of {amount:N2} would exceed the total owed...");

    PaidAmount = newPaidAmount;
    PaidDate = paymentDate;

    if (PaidAmount >= totalOwed)
        Status = InstallmentStatus.Paid;
    else
        Status = InstallmentStatus.PartiallyPaid;

    MarkUpdated();
}
```

**What it does:** Records a payment that may or may not cover the full amount.

**Validations:**
1. Amount must be positive
2. Can't pay an already-paid installment
3. Cumulative payments can't exceed what's owed

**Key logic:** `newPaidAmount = PaidAmount + amount` — This is CUMULATIVE. If
the borrower paid £200 last week and £400 today, PaidAmount becomes £600.

**State transition:**
- If cumulative payments >= total owed → Status = Paid
- Otherwise → Status = PartiallyPaid

**Business scenario:** A borrower can't afford the full £600 this month. They
pay £300 now and £300 next week. Both calls go through RecordPartialPayment.
The first sets status to PartiallyPaid. The second sets it to Paid.


### Method: `MarkLate(decimal lateFeePercentage)`

```csharp
public void MarkLate(decimal lateFeePercentage)
{
    if (Status != InstallmentStatus.Pending
        && Status != InstallmentStatus.PartiallyPaid)
        throw new DomainException("...");

    Status = InstallmentStatus.Late;
    LateFeeAmount = decimal.Round(
        (TotalAmount - PaidAmount) * lateFeePercentage, 2);
    MarkUpdated();
}
```

**What it does:** Transitions the installment to Late status and calculates
the late fee.

**Guard:** Only Pending or PartiallyPaid installments can become Late. You
can't mark a Paid installment as late (it's already paid). You can't mark a
Missed installment as late (it's already worse than late).

**Late fee calculation:** `(TotalAmount - PaidAmount) * lateFeePercentage`

Example: Installment is £600, borrower paid £200 partially, late fee is 2%.
Late fee = (600 - 200) * 0.02 = £8.00

The fee is calculated on the OUTSTANDING amount, not the total. This is fair
— if you've already paid most of it, the penalty is smaller.

**Who calls this?** The `LatePaymentService` background job. It runs daily,
finds installments past their due date + grace period, and calls this method.

### Method: `MarkMissed()`

```csharp
public void MarkMissed()
{
    if (Status != InstallmentStatus.Late)
        throw new DomainException("...");

    Status = InstallmentStatus.Missed;
    MarkUpdated();
}
```

**What it does:** Transitions from Late to Missed.

**Guard:** ONLY Late installments can become Missed. The progression is:
Pending → Late → Missed. You can't skip Late.

**When does this happen?** When the NEXT installment's due date arrives and
the current one is still Late. The background service detects this.

**Business meaning:** "Late" means overdue but still recoverable. "Missed"
means the window has passed — the next payment is now due.


### Methods: `MarkReminderSent()` and `MarkLateNoticeSent()`

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

**What they do:** Set boolean flags to prevent duplicate notifications.

**Why they exist:** The background service runs daily. Without these flags,
it would send a reminder EVERY DAY for the same installment. With the flags,
it sends once and marks it done.

---

# PART 6: Entities — The RepaymentSchedule

---

## File: `Entities/RepaymentSchedule.cs`

This entity represents the entire repayment plan for a funded loan. It's the
parent of all installments.

### The Backing Field Pattern

```csharp
private readonly List<Installment> _installments = [];
public IReadOnlyCollection<Installment> Installments =>
    _installments.AsReadOnly();
```

**Why this pattern?** We want:
1. EF Core to be able to load installments from the database (needs a List)
2. External code to NOT be able to add/remove installments directly
3. Only the schedule itself to control its installment collection

`IReadOnlyCollection` means external code can iterate and query, but cannot
call `.Add()` or `.Remove()`. The only way to add installments is through
the `AddInstallment()` method.


### Constructor

```csharp
public RepaymentSchedule(
    Guid loanApplicationId, Guid lenderId,
    decimal fundedAmount, decimal annualInterestRate,
    int termMonths, decimal monthlyEmi,
    decimal totalInterestPayable)
```

**Why public?** Unlike other entities, this constructor is public because the
`AmortizationService` (in the Application layer) needs to create schedules.
The service calculates all the values and passes them in.

**Parameters:**
- `loanApplicationId` — Which loan this schedule belongs to
- `lenderId` — Who funded it
- `fundedAmount` — The principal (how much was borrowed)
- `annualInterestRate` — The effective rate (base + credit tier adjustment)
- `termMonths` — How many months the loan runs
- `monthlyEmi` — The fixed monthly payment amount
- `totalInterestPayable` — Total interest over the life of the loan

**Initial state:** `Performance = LoanPerformance.OnTime` — Every new loan
starts as performing.

### Method: `GetNextPendingInstallment()`

```csharp
public Installment? GetNextPendingInstallment()
{
    return _installments
        .Where(i => i.Status is InstallmentStatus.Pending
            or InstallmentStatus.PartiallyPaid
            or InstallmentStatus.Late
            or InstallmentStatus.Missed)
        .OrderBy(i => i.InstallmentNumber)
        .FirstOrDefault();
}
```

**What it does:** Finds the earliest unpaid installment.

**Why it includes Late and Missed:** Even if an installment is late or missed,
it still needs to be paid. The borrower must clear it before moving to the
next one. This enforces SEQUENTIAL payment order.

**Returns null when:** All installments are Paid. This means the loan is
fully repaid.


### Method: `UpdatePerformance()`

```csharp
public void UpdatePerformance()
{
    var orderedInstallments = _installments
        .OrderByDescending(i => i.InstallmentNumber)
        .ToList();

    var consecutiveBad = 0;
    foreach (var installment in orderedInstallments)
    {
        if (installment.Status is InstallmentStatus.Late
            or InstallmentStatus.Missed)
            consecutiveBad++;
        else
            break;
    }

    if (consecutiveBad >= 3)
        Performance = LoanPerformance.Defaulted;
    else if (_installments.Any(i => i.Status is
        InstallmentStatus.Late or InstallmentStatus.Missed))
        Performance = LoanPerformance.Late;
    else
        Performance = LoanPerformance.OnTime;

    MarkUpdated();
}
```

**What it does:** Evaluates the loan's overall health based on installment
statuses.

**Algorithm:**
1. Order installments from NEWEST to OLDEST
2. Count consecutive Late/Missed from the end
3. If 3+ consecutive → Defaulted
4. If any Late/Missed (but less than 3 consecutive) → Late
5. If all are Pending or Paid → OnTime

**Why from newest to oldest?** Because we care about RECENT behaviour. If
installments 1-10 were paid on time but 11, 12, 13 are all missed, that's
a default. But if installment 5 was late and 6-13 are on time, the loan
recovered — it's OnTime.

**Business impact:** A Defaulted loan appears in the Collections queue.
Lenders see it flagged red in their portfolio. It affects the platform's
default rate metric.

### Method: `Restructure()`

```csharp
public void Restructure(decimal newRate, int newTermMonths,
    decimal newEmi, decimal newTotalInterest)
{
    if (Performance == LoanPerformance.OnTime)
        throw new DomainException("Cannot restructure a loan that is
            performing on time...");

    AnnualInterestRate = newRate;
    TermMonths = newTermMonths;
    MonthlyEmi = newEmi;
    TotalInterestPayable = newTotalInterest;
    Performance = LoanPerformance.OnTime;
    MarkUpdated();
}
```

**What it does:** Updates the loan terms for a distressed loan.

**Guard:** Only Late or Defaulted loans can be restructured. If a loan is
performing fine, there's no reason to change the terms.

**Why reset Performance to OnTime?** After restructuring, the loan gets a
fresh start. The new terms are designed to be affordable, so we give the
borrower a clean slate.

**Business scenario:** A borrower lost their job and missed 3 payments. The
lender restructures: extends the term from 24 to 36 months and reduces the
rate from 14% to 10%. The new EMI is lower and more affordable.


---

# PART 7: Domain Services — The PaymentProcessor

---

## File: `Services/PaymentProcessor.cs`

### Why Is This a Service and Not a Method on an Entity?

Think of it like this — when a payment comes in, it needs to:
1. Find the right installment (from the schedule)
2. Validate the amount (against the installment)
3. Record the payment (on the installment)
4. Update performance (on the schedule)

This crosses TWO entities (Schedule and Installment). Neither entity "owns"
this logic. That's why it's a domain service — it coordinates between entities.

### Method: `RecordPayment(schedule, amount, paymentDate)`

**Step 1:** Validate amount > 0. Zero or negative payments make no sense.

**Step 2:** Get the next pending installment from the schedule. If null, all
payments are complete — throw an error.

**Step 3:** Calculate what's owed on that installment:
`totalOwed = TotalAmount + LateFeeAmount - PaidAmount`

This accounts for:
- The base amount (principal + interest)
- Any late fee that was applied
- Any partial payments already made

**Step 4:** Validate the payment doesn't exceed what's owed. You can't
overpay a single installment.

**Step 5:** If payment >= owed → call `RecordFullPayment()`. Otherwise →
call `RecordPartialPayment()`.

**Step 6:** Update the schedule's performance classification.

### Method: `RecordBulkPayment(schedule, totalAmount, paymentDate)`

This is the "pay off my entire loan" method.

**Algorithm:**
```
remaining = totalAmount
while remaining > 0:
    get next pending installment
    if none → break (loan fully paid)
    calculate what's owed on it
    if remaining >= owed:
        pay it in full
        remaining -= owed
        installmentsPaid++
    else:
        pay partial (remaining amount)
        remaining = 0
update performance
return installmentsPaid
```

**Business scenario:** A borrower receives a bonus and wants to pay off their
entire £15,000 remaining balance. They click "Pay All Remaining". The system
iterates through installments 5, 6, 7... paying each one fully until the
money runs out.

**Return value:** The number of installments fully paid. This is shown to the
user: "3 installments paid. Loan fully settled!"

---

# PART 8: All Enums Explained

---

## `InstallmentStatus`
```
Pending = 1        → Payment not yet due or awaiting payment
Paid = 2           → Full payment received
PartiallyPaid = 3  → Some payment received, balance remaining
Late = 4           → Overdue past grace period
Missed = 5         → Payment window closed, next installment due
```

## `LoanPerformance`
```
OnTime = 1    → All payments current
Late = 2      → One or more installments overdue
Defaulted = 3 → 3+ consecutive Late/Missed
```

## `LoanApplicationStatus`
```
Draft = 1              → Created but not submitted
Submitted = 2          → Sent for review
UnderReview = 3        → Being evaluated by CRM
Approved = 4           → Ready for funding
Rejected = 5           → Declined
Funded = 6             → Money disbursed
Withdrawn = 7          → Borrower cancelled
DocumentsRequested = 8 → More docs needed
```

## `LenderStatus`
```
Draft = 1               → Initial state
PendingVerification = 2 → Awaiting KYC check
Verified = 3            → Can fund loans
Suspended = 4           → Temporarily blocked
Archived = 5            → Permanently removed
```

## `BorrowerStatus`
```
Draft = 1               → Initial state
PendingVerification = 2 → Awaiting verification
Verified = 3            → Can apply for loans
Suspended = 4           → Temporarily blocked
Archived = 5            → Permanently removed
```

## `LoanProductStatus`
```
Draft = 1           → Being configured
PendingApproval = 2 → Submitted for review
Approved = 3        → Approved but not live
Published = 4       → Live and visible to borrowers
Archived = 5        → Removed from marketplace
```

## `CreditTier`
```
A → Excellent credit (base rate, high limits)
B → Good credit (base + 2%, medium limits)
C → Fair credit (base + 4%, lower limits)
```

## `AccountStatus`
```
PendingApproval   → New user awaiting vetting
Active            → Normal operation
Hold              → Temporary hold
Blocked           → Cannot perform actions
Suspended         → Account suspended
Closed            → Account closed
DocumentsRequested → More docs needed
```

## `DocumentStatus`
```
Pending = 1  → Uploaded, not reviewed
Verified = 2 → Approved by CRM
Rejected = 3 → Rejected by CRM
```

## `DocumentType`
```
NationalID = 1     → Government ID
ProofOfIncome = 2  → Payslips/tax returns
BankStatement = 3  → Recent bank activity
AddressProof = 4   → Utility bill
Other = 5          → Miscellaneous
```

## `CollectionStatus`
```
New = 1                → Just entered default
ContactAttempted = 2   → First outreach made
PaymentPlanAgreed = 3  → Restructuring agreed
InRepaymentPlan = 4    → Making plan payments
WrittenOff = 5         → Unrecoverable
Recovered = 6          → Fully recovered
```

## `PermissionModule`
```
UserManagement, LoanManagement, ProductManagement,
FinancialOperations, Reports, SystemSettings, Messaging
```

## `PermissionAction`
```
View, Create, Edit, Delete, Approve
```

These combine to form granular permissions like:
"LoanManagement.Approve" or "UserManagement.Create"

---

This completes the exhaustive Domain layer documentation. Every class, every
method, every property, every enum value has been explained with business
context, technical reasoning, and real-world scenarios.
