# Design Patterns — The Complete Bible

## Every Pattern, Where It Lives, Why It Was Chosen

---

## 1. Clean Architecture

**Where:** Solution structure — 6 projects with strict dependency direction.

**The rule:** Dependencies point INWARD. Domain depends on nothing.
Application depends on Domain. Infrastructure depends on Application + Domain.
API depends on all.

**Why:** If we need to swap SQL Server for PostgreSQL, only Infrastructure
changes. If we add a mobile app, we add a new presentation layer — handlers
stay the same.

**Evidence in code:** The Domain.csproj has zero PackageReferences to EF Core
or ASP.NET. It only references `Microsoft.Extensions.Identity.Stores` for
the Identity base classes.

---

## 2. CQRS (Command Query Responsibility Segregation)

**Where:** Every file in `Application/Features/`

**The rule:** Operations that CHANGE state (Commands) are completely separate
from operations that READ state (Queries). They never share code.

**Why:**
- Commands need transactions, validation, audit trails
- Queries need caching, projections, read replicas
- Separating them lets you optimize each independently

**Evidence:** `FundLoanCommand` changes 3 tables. `GetFundingQueueQuery` only
reads. They have different security, different caching, different performance
characteristics.

---

## 3. MediatR Pipeline Behaviours

**Where:** `Application/Common/Behaviours/` — 7 behaviours registered.

**The rule:** Cross-cutting concerns are applied automatically to every
request via the pipeline, not manually in each handler.

**Why:** Without this, every handler would need:
```csharp
// Validate
// Check account status
// Check limits
// Log
// Time it
// Check cache
// Scope data
// THEN do the actual work
```

With behaviours, the handler only contains business logic.

---

## 4. Repository Pattern

**Where:** Interfaces in `Application/Common/Interfaces/`, implementations
in `Infrastructure/Repositories/`

**The rule:** The Application layer defines WHAT data access it needs. The
Infrastructure layer defines HOW.

**Why:** Handlers can be unit tested with mocked repositories. No database
needed for testing business logic.

---

## 5. Factory Method Pattern

**Where:** Every entity's `Create()` static method.

**The rule:** Objects are created through a factory that validates inputs,
not through public constructors.

**Why:** Prevents invalid objects from existing. You literally cannot create
a Lender with a negative balance — the factory rejects it.

---

## 6. State Machine Pattern

**Where:** `LoanApplication`, `Installment`, `LoanProduct`, `Lender`,
`Borrower`, `ApplicationDocument`

**The rule:** Entities have explicit states and guarded transitions. You
can't skip states or make invalid transitions.

**Why:** Prevents impossible business scenarios. A loan can't be funded
without being approved first. An installment can't be missed without being
late first.

---

## 7. Value Object Pattern

**Where:** `Money`, `InterestRate`

**The rule:** Immutable objects defined by their value, not identity. Two
Money(100, GBP) are equal regardless of which variable holds them.

**Why:** Prevents bugs like comparing £100 to $100 (different currencies).
Prevents mutation bugs (changing a shared Money object).

---

## 8. Domain Service Pattern

**Where:** `PaymentProcessor`

**The rule:** Logic that doesn't belong to a single entity lives in a
domain service.

**Why:** Payment processing coordinates between Schedule and Installment.
Neither entity "owns" this logic.

---

## 9. Specification Pattern

**Where:** `Application/Common/Specifications/`

**The rule:** Query predicates are encapsulated in reusable objects.

**Why:** Instead of writing `Where(s => s.Performance != Defaulted)` in
multiple places, define `ActiveSchedulesSpecification` once and reuse it.

---

## 10. Domain Events (Pub/Sub)

**Where:** `Application/Common/Events/`

**The rule:** When something important happens, publish an event. Interested
parties subscribe and react independently.

**Why:** The FundLoanHandler shouldn't know about SignalR notifications. It
publishes `LoanFundedEvent` and the `LoanFundedEventHandler` deals with
notifications separately.

---

## 11. Cache-Aside Pattern

**Where:** `CachingBehaviour` + `ICacheableQuery`

**The rule:** Check cache → if miss, execute query → store result → return.

**Why:** Dashboard queries aggregate multiple tables. Caching for 2 minutes
reduces database load by 95% for frequently-viewed pages.

---

## 12. Middleware Pattern

**Where:** `CorrelationIdMiddleware`, `GlobalExceptionMiddleware`

**The rule:** HTTP concerns that apply to ALL requests are handled in
middleware, not in individual controllers.

**Why:** Every request needs a correlation ID. Every request needs exception
handling. Middleware ensures nothing is missed.

---

## 13. Options Pattern

**Where:** `RepaymentSettings`, `JwtSettings`, `AccountSettings`

**The rule:** Configuration is bound to strongly-typed classes and injected
via `IOptions<T>`.

**Why:** No magic strings. No `Configuration["RepaymentSettings:GracePeriodDays"]`.
Just `_settings.GracePeriodDays` with compile-time safety.

---

## 14. Hosted Service Pattern

**Where:** `LatePaymentHostedService`

**The rule:** Background work runs on a timer, independent of HTTP requests.

**Why:** Late payment detection must happen daily regardless of whether
anyone visits the website. It's a scheduled job, not a user-triggered action.

---

## 15. Dependency Injection

**Where:** Every constructor in the application.

**The rule:** Classes declare dependencies via constructor parameters. The
DI container resolves them at runtime.

**Why:** Testability (inject mocks), flexibility (swap implementations),
lifetime management (scoped per request vs singleton).

---

## 16. Marker Interface Pattern

**Where:** `IResourceFilteredQuery`, `ICacheableQuery`, `ILoanFundingCommand`

**The rule:** Empty interfaces that signal behaviour to pipeline components.

**Why:** The CachingBehaviour checks `if (request is ICacheableQuery)`. If
yes, it caches. If no, it skips. No configuration needed — just implement
the interface.

---

## 17. Backing Field Pattern (EF Core)

**Where:** `RepaymentSchedule._installments`

**The rule:** The public property is read-only (`IReadOnlyCollection`). EF
Core reads/writes through the private backing field.

**Why:** External code can't add installments directly to the collection.
They must go through `AddInstallment()` which the entity controls.
