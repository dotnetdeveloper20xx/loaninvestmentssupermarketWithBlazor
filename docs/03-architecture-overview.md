# Architecture Overview

## The Big Picture

Think of this application like a layered cake. Each layer has a specific job, and they only talk to the layer directly below them. This is called **Clean Architecture** — and the reason it exists is to keep the code maintainable as it grows.

```
┌─────────────────────────────────────────────┐
│           Blazor WebAssembly (UI)           │  ← What users see and interact with
├─────────────────────────────────────────────┤
│          Typed API Clients                  │  ← How the UI talks to the backend
├─────────────────────────────────────────────┤
│          ASP.NET Core API                   │  ← HTTP endpoints, auth, middleware
├─────────────────────────────────────────────┤
│     MediatR Pipeline (Behaviours)           │  ← Cross-cutting: validation, logging, caching
├─────────────────────────────────────────────┤
│     Application Layer (CQRS Handlers)       │  ← Business logic orchestration
├─────────────────────────────────────────────┤
│     Domain Layer (Entities + Services)      │  ← Core business rules, state machines
├─────────────────────────────────────────────┤
│     Infrastructure (EF Core + Services)     │  ← Database access, external services
├─────────────────────────────────────────────┤
│          SQL Server Database                │  ← Where data lives permanently
└─────────────────────────────────────────────┘
```

---

## Why Clean Architecture?

The reason this exists is simple: **change is inevitable**.

- Business rules change → only the Domain layer changes
- Database technology changes → only Infrastructure changes
- UI framework changes → only the Blazor project changes
- New API consumers appear → add a new controller, handlers stay the same

No layer knows about the layers above it. The Domain layer has ZERO dependencies on anything else. This is called the **Dependency Inversion Principle** — and it's the most important architectural decision in this project.

---

## Solution Structure

```
src/
├── LoanSuperMarket.Api/            ← ASP.NET Core Web API (entry point)
├── LoanSuperMarket.Application/    ← CQRS handlers, services, interfaces
├── LoanSuperMarket.Domain/         ← Entities, value objects, enums, domain services
├── LoanSuperMarket.Infrastructure/ ← EF Core, repositories, identity, Dapper
├── LoanSuperMarket.Shared/         ← DTOs, requests, configuration (shared with Blazor)
└── LoanSuperMarket.Blazor/         ← Blazor WebAssembly frontend

tests/
├── LoanSuperMarket.Domain.Tests/      ← Unit tests for domain logic
├── LoanSuperMarket.Application.Tests/ ← Unit tests for application handlers
└── LoanSuperMarket.Api.Tests/         ← Integration tests
```

---

## Layer-by-Layer Explanation

### Domain Layer (`LoanSuperMarket.Domain`)

**What it is:** The heart of the application. Contains business rules that are true regardless of what UI or database you use.

**What's inside:**
- `Entities/` — Rich domain objects with behaviour (not just data bags)
- `ValueObjects/` — Immutable types like Money and InterestRate
- `Enums/` — Business status codes (LoanApplicationStatus, InstallmentStatus, etc.)
- `Common/` — Base classes (AuditableEntity, DomainException)
- `Services/` — Domain services (PaymentProcessor)

**Key principle:** Entities protect their own invariants. You can't set a loan to "Funded" without going through the `Fund()` method, which checks that the current status is "Approved". This prevents invalid state transitions.

---

### Application Layer (`LoanSuperMarket.Application`)

**What it is:** The orchestrator. It coordinates domain objects, repositories, and services to fulfill business use cases.

**What's inside:**
- `Features/` — Organized by business feature (Funding, Payments, Dashboard, etc.)
- `Common/Behaviours/` — MediatR pipeline behaviours (validation, logging, caching, auth)
- `Common/Interfaces/` — Abstractions for infrastructure (repositories, services)
- `Common/Events/` — Domain events (LoanFundedEvent, PaymentRecordedEvent)
- `Common/Specifications/` — Query specification pattern

**Key pattern: CQRS (Command Query Responsibility Segregation)**

Every operation is either a **Command** (changes state) or a **Query** (reads state). They're never mixed.

```
Command: FundLoanCommand → FundLoanCommandHandler → changes database
Query:   GetFundingQueueQuery → GetFundingQueueQueryHandler → reads database
```

Why? Because reads and writes have different performance characteristics, different scaling needs, and different security requirements.

---

### Infrastructure Layer (`LoanSuperMarket.Infrastructure`)

**What it is:** The implementation details. How we actually talk to databases, send emails, manage identity.

**What's inside:**
- `Persistence/` — EF Core DbContext, entity configurations, migrations, design-time factory
- `Persistence/Configurations/` — Fluent API configurations for each entity
- `Persistence/StoredProcedures/` — SQL stored procedures
- `Repositories/` — Repository implementations
- `Services/` — External service implementations (notifications, Dapper reports, user queries)
- `Identity/` — ASP.NET Identity setup, JWT token service

**Key principle:** The Application layer defines interfaces (e.g., `ILenderRepository`). The Infrastructure layer implements them (e.g., `LenderRepository`). This means you could swap SQL Server for PostgreSQL by only changing this layer.

---

### API Layer (`LoanSuperMarket.Api`)

**What it is:** The HTTP entry point. Receives requests, dispatches them to MediatR, returns responses.

**What's inside:**
- `Controllers/` — REST endpoints grouped by domain area
- `Middleware/` — Global exception handling, correlation IDs
- `Hubs/` — SignalR hub for real-time notifications
- `Services/` — SignalR notifier implementation
- `Program.cs` — Application startup, DI configuration, middleware pipeline

**Key principle:** Controllers are thin. They don't contain business logic. They just:
1. Receive the HTTP request
2. Map it to a command/query
3. Send it through MediatR
4. Return the response

---

### Shared Layer (`LoanSuperMarket.Shared`)

**What it is:** DTOs and contracts shared between the API and the Blazor frontend. Both projects reference this.

**What's inside:**
- `Common/` — ApiResponse<T>, PagedResult<T>
- `Funding/` — FundingQueueItemDto, FundingResultDto, etc.
- `Payments/` — RepaymentScheduleDto, InstallmentDto, etc.
- `Dashboard/` — LenderPortfolioDto, BorrowerLoanDto, etc.
- `Configuration/` — RepaymentSettings, NotificationPreferencesDto

**Why it exists:** So the Blazor frontend can deserialize API responses into strongly-typed objects without duplicating DTO definitions.

---

### Blazor Layer (`LoanSuperMarket.Blazor`)

**What it is:** The single-page application that runs in the browser via WebAssembly.

**What's inside:**
- `Pages/` — Routable pages (each has a `@page` directive)
- `Components/` — Reusable UI components organized by domain
- `Layout/` — MainLayout with sidebar navigation
- `Services/` — API clients, auth, theme, SignalR client
- `Program.cs` — WASM startup, DI registration

**Key principle:** Pages are thin. They inject API clients, call methods, and render components. Business logic lives on the server.

---

## The MediatR Pipeline

When a request comes in, it flows through a pipeline of behaviours before reaching the handler:

```
Request → LoggingBehaviour → PerformanceBehaviour → ValidationBehaviour 
        → CachingBehaviour → AccountStatusBehaviour → LimitEnforcementBehaviour 
        → ResourceAuthorizationBehaviour → HANDLER → Response
```

Each behaviour is a cross-cutting concern:
- **Logging** — Records every request for debugging
- **Performance** — Warns if a handler takes too long
- **Validation** — Runs FluentValidation rules, rejects invalid requests
- **Caching** — Returns cached results for ICacheableQuery requests
- **Account Status** — Blocks suspended/archived users
- **Limit Enforcement** — Checks credit limits and capital limits
- **Resource Authorization** — Scopes data by user role (lenders see their loans only)

This is why you don't see validation logic in handlers — it's already been done by the time the handler runs.

---

## Database Strategy

The application uses TWO database contexts:

1. **ApplicationDbContext** — Business data (loans, schedules, installments, products)
2. **AuthIdentityDbContext** — Identity data (users, roles, tokens, sessions)

Both point to the same SQL Server database but are separated for architectural clarity.

For high-performance reporting, **Dapper** is used alongside EF Core to call stored procedures directly. This avoids the overhead of EF Core's change tracking for read-only aggregation queries.

---

## Real-Time Communication

**SignalR** provides real-time push notifications:
- When a loan is funded → all lenders get "FundingQueueChanged"
- When a payment is recorded → the borrower gets "PaymentRecorded"
- The Blazor client auto-connects on login and subscribes to events

This means the funding queue refreshes automatically when another lender funds a loan — no manual refresh needed.
