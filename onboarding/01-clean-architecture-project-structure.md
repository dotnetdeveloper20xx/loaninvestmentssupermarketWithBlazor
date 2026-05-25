# Clean Architecture Project Structure

> **Audience:** Mid-level C# developer joining the Loan Investment Supermarket team.
> After reading this document, you should understand how the codebase is organized, why it's organized that way, and where to put new code.

---

## Table of Contents

1. [What is Clean Architecture?](#what-is-clean-architecture)
2. [Technologies & Patterns](#technologies--patterns)
3. [Solution Structure Overview](#solution-structure-overview)
4. [Project-by-Project Breakdown](#project-by-project-breakdown)
5. [Dependency Flow Diagram](#dependency-flow-diagram)
6. [Key Files with Code Snippets](#key-files-with-code-snippets)
7. [How to Add a New Feature](#how-to-add-a-new-feature)
8. [Key Rules](#key-rules)

---

## What is Clean Architecture?

Clean Architecture (coined by Robert C. Martin, a.k.a. "Uncle Bob") is a way of organizing code so that:

- **Business logic is isolated** from frameworks, databases, and UI.
- **Dependencies point inward** — outer layers know about inner layers, never the reverse.
- **You can swap infrastructure** (e.g., replace SQL Server with PostgreSQL) without touching business rules.
- **Testing is easy** because business logic has no dependency on external systems.

### The Dependency Rule

This is the single most important rule:

> **Inner layers NEVER reference outer layers.**

Think of it as concentric circles:

```
┌─────────────────────────────────────────────────────────────┐
│                        API / Blazor                          │  ← Outermost (presentation)
│  ┌───────────────────────────────────────────────────────┐  │
│  │                   Infrastructure                       │  │  ← Frameworks & DB
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │                 Application                      │  │  │  ← Use cases & orchestration
│  │  │  ┌───────────────────────────────────────────┐  │  │  │
│  │  │  │                 Domain                     │  │  │  │  ← Core business rules
│  │  │  └───────────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

- **Domain** knows nothing about the outside world.
- **Application** knows about Domain, but not about databases or HTTP.
- **Infrastructure** implements what Application defines (interfaces → concrete classes).
- **API/Blazor** are thin shells that wire everything together and expose it to users.

### Why We Use It

1. **Testability** — Domain and Application can be unit-tested without a database or web server.
2. **Flexibility** — Swap EF Core for Dapper, SQL Server for PostgreSQL, REST for gRPC — all without touching business logic.
3. **Onboarding** — New developers know exactly where to find things.
4. **Longevity** — The architecture scales as the project grows without becoming a tangled mess.

---

## Technologies & Patterns

| Technology / Pattern | Purpose |
|---------------------|---------|
| **.NET 10** | Runtime and SDK (latest preview) |
| **.slnx format** | New XML-based solution file format (replaces `.sln`) |
| **Clean Architecture** | Structural pattern separating concerns into layers |
| **MediatR** | CQRS (Command Query Responsibility Segregation) via in-process messaging |
| **FluentValidation** | Declarative request validation in the Application layer |
| **Entity Framework Core** | ORM for SQL Server (Infrastructure layer) |
| **Dapper** | Lightweight data access for stored procedure reporting |
| **ASP.NET Core Identity** | Authentication & user management |
| **JWT Bearer Tokens** | Stateless API authentication |
| **SignalR** | Real-time notifications (WebSocket hub) |
| **Blazor WebAssembly** | SPA frontend running in the browser |
| **Tailwind CSS + DaisyUI** | Utility-first styling for the Blazor frontend |
| **Dependency Injection** | Built-in .NET DI container wires all layers together |
| **Interface Segregation** | Application defines focused interfaces; Infrastructure implements them |
| **Pipeline Behaviours** | Cross-cutting concerns (validation, logging, caching) as MediatR middleware |

---

## Solution Structure Overview

The solution file (`LoanSuperMarketUsingBlazor.slnx`) organizes projects into two folders:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/LoanSuperMarket.Api/LoanSuperMarket.Api.csproj" />
    <Project Path="src/LoanSuperMarket.Application/LoanSuperMarket.Application.csproj" />
    <Project Path="src/LoanSuperMarket.Blazor/LoanSuperMarket.Blazor.csproj" />
    <Project Path="src/LoanSuperMarket.Domain/LoanSuperMarket.Domain.csproj" />
    <Project Path="src/LoanSuperMarket.Infrastructure/LoanSuperMarket.Infrastructure.csproj" />
    <Project Path="src/LoanSuperMarket.Shared/LoanSuperMarket.Shared.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/LoanSuperMarket.Api.Tests/LoanSuperMarket.Api.Tests.csproj" />
    <Project Path="tests/LoanSuperMarket.Application.Tests/LoanSuperMarket.Application.Tests.csproj" />
    <Project Path="tests/LoanSuperMarket.Domain.Tests/LoanSuperMarket.Domain.Tests.csproj" />
  </Folder>
</Solution>
```

---

## Project-by-Project Breakdown

### 1. `LoanSuperMarket.Domain` — The Core

**References:** None (zero external project dependencies)
**Target:** `net10.0`

This is the heart of the system. It contains pure business rules with no knowledge of databases, HTTP, or frameworks.

```
LoanSuperMarket.Domain/
├── Common/
│   ├── BaseEntity.cs            ← All entities inherit from this (provides Id)
│   ├── AuditableEntity.cs       ← Adds CreatedAtUtc, UpdatedAtUtc tracking
│   └── DomainException.cs       ← Base class for domain-specific exceptions
├── Entities/
│   ├── Identity/                ← ApplicationUser, CustomRole (ASP.NET Identity entities)
│   ├── Borrower.cs
│   ├── Lender.cs
│   ├── LoanProduct.cs
│   ├── LoanApplication.cs
│   ├── Installment.cs
│   ├── RepaymentSchedule.cs
│   ├── ApplicationDocument.cs
│   └── AuditLog.cs
├── Enums/
│   ├── LoanApplicationStatus.cs
│   ├── BorrowerStatus.cs
│   ├── LenderStatus.cs
│   ├── InstallmentStatus.cs
│   └── ... (13 enum files)
├── ValueObjects/
│   ├── Money.cs                 ← Encapsulates monetary amounts with validation
│   └── InterestRate.cs          ← Encapsulates rate with range validation
├── Services/
│   ├── IPaymentProcessor.cs     ← Domain service interface
│   └── PaymentProcessor.cs      ← Domain service implementation
└── Events/                      ← (Placeholder for domain events)
```

**Key design decisions:**
- Entities use factory methods (`LoanProduct.Create(...)`) instead of public constructors
- Value Objects enforce invariants at creation time (e.g., `Money` can't be negative)
- The only NuGet package is `Microsoft.Extensions.Identity.Stores` (needed for Identity entity base classes)

---

### 2. `LoanSuperMarket.Application` — Use Cases & Orchestration

**References:** `LoanSuperMarket.Domain`, `LoanSuperMarket.Shared`
**Target:** `net10.0`
**Key NuGet:** MediatR, FluentValidation

This layer defines *what the system does* (use cases) without knowing *how* it's done (no database code here).

```
LoanSuperMarket.Application/
├── Common/
│   ├── Behaviours/
│   │   ├── LoggingBehaviour.cs          ← Logs every request
│   │   ├── PerformanceBehaviour.cs      ← Warns on slow handlers
│   │   ├── ValidationBehaviour.cs       ← Runs FluentValidation before handler
│   │   ├── CachingBehaviour.cs          ← Response caching pipeline
│   │   ├── AccountStatusBehaviour.cs    ← Blocks suspended accounts
│   │   ├── LimitEnforcementBehaviour.cs ← Enforces business limits
│   │   └── ResourceAuthorizationBehaviour.cs ← Resource-level auth checks
│   ├── Events/                          ← Application-level event handling
│   ├── Exceptions/                      ← Application-specific exceptions
│   ├── Interfaces/                      ← 29 interfaces (see below)
│   ├── Models/                          ← Shared result types
│   └── Specifications/                  ← Query specification pattern
├── Features/
│   ├── Audit/
│   ├── Auth/
│   ├── Borrowers/
│   ├── Credit/
│   ├── Dashboard/
│   ├── Funding/
│   ├── Lenders/
│   ├── LoanApplications/
│   ├── LoanProducts/
│   │   ├── CreateLoanProduct/
│   │   │   ├── CreateLoanProductCommand.cs
│   │   │   ├── CreateLoanProductCommandHandler.cs
│   │   │   └── CreateLoanProductCommandValidator.cs
│   │   ├── GetLoanProducts/
│   │   ├── GetLoanProductById/
│   │   └── ... (8 feature folders)
│   ├── Notifications/
│   ├── Payments/
│   ├── Roles/
│   ├── Sessions/
│   ├── Users/
│   └── Vetting/
└── DependencyInjection.cs
```

**CQRS Pattern:** Each feature is a folder containing:
- A **Command** or **Query** (the request DTO, implements `IRequest<T>`)
- A **Handler** (the business logic, implements `IRequestHandler<TRequest, TResponse>`)
- A **Validator** (optional, FluentValidation rules)

**Interface Segregation:** The `Common/Interfaces/` folder defines 29 focused interfaces:
- `ILoanProductRepository` — CRUD for loan products
- `IBorrowerRepository` — CRUD for borrowers
- `IIdentityService` — User registration, login, password management
- `ITokenService` — JWT token generation
- `IEmailService` — Email sending (abstracted)
- `INotificationService` — Push notifications (abstracted)
- `IDocumentStorageService` — File storage (abstracted)
- ... and more

The Application layer *defines* these interfaces. Infrastructure *implements* them. This is the core of Dependency Inversion.

---

### 3. `LoanSuperMarket.Infrastructure` — The "How"

**References:** `LoanSuperMarket.Application`, `LoanSuperMarket.Domain`
**Target:** `net10.0`
**Key NuGet:** EF Core (SQL Server), Dapper, ASP.NET Identity, JWT

This layer implements everything the Application layer defines. It's the only layer that talks to databases, file systems, and external services.

```
LoanSuperMarket.Infrastructure/
├── Identity/
│   ├── AuthIdentityDbContext.cs     ← Separate DbContext for Identity tables
│   ├── AuthorizationPolicies.cs     ← Policy definitions (role-based, permission-based)
│   ├── CurrentUserService.cs        ← Implements ICurrentUserService (reads HttpContext)
│   ├── IdentitySeeder.cs            ← Seeds default roles and admin account
│   ├── IdentityService.cs           ← Implements IIdentityService
│   ├── JwtTokenService.cs           ← Implements ITokenService (JWT generation)
│   ├── PermissionResolver.cs        ← Implements IPermissionResolver
│   ├── RoleManagementService.cs     ← Implements IRoleManagementService
│   ├── SessionService.cs            ← Implements ISessionService
│   └── TwoFactorService.cs          ← Implements ITwoFactorService
├── Migrations/
│   ├── AuthIdentityDb/              ← Identity-specific migrations
│   └── 20260525084158_InitialCreate.cs
├── Persistence/
│   ├── Configurations/              ← EF Core entity configurations (Fluent API)
│   ├── StoredProcedures/            ← SQL stored procedure scripts
│   ├── ApplicationDbContext.cs      ← Main DbContext for business entities
│   ├── ApplicationDbContextFactory.cs ← Design-time factory for migrations
│   └── DevelopmentDataSeeder.cs     ← Seeds sample data for development
├── Repositories/
│   ├── BorrowerRepository.cs        ← Implements IBorrowerRepository
│   ├── LenderRepository.cs          ← Implements ILenderRepository
│   ├── LoanProductRepository.cs     ← Implements ILoanProductRepository
│   ├── LoanApplicationRepository.cs ← Implements ILoanApplicationRepository
│   ├── DashboardRepository.cs       ← Implements IDashboardRepository
│   ├── AuditLogRepository.cs        ← Implements IAuditLogRepository
│   └── ApplicationDocumentRepository.cs
├── Services/
│   ├── AmortizationService.cs       ← Loan amortization calculations
│   ├── DapperPlatformReportService.cs ← Stored procedure reporting via Dapper
│   ├── LatePaymentHostedService.cs  ← Background service for late payment detection
│   ├── NoOpEmailService.cs          ← Stub email (dev environment)
│   ├── StubNotificationService.cs   ← Stub notifications (dev environment)
│   ├── StubDocumentStorageService.cs ← Stub file storage (dev environment)
│   ├── ClientInfoProvider.cs        ← IP/User-Agent extraction
│   ├── RoleQueryService.cs          ← Implements IRoleQueryService
│   └── UserQueryService.cs          ← Implements IUserQueryService
└── DependencyInjection.cs           ← Registers ALL infrastructure services
```

**Key design decisions:**
- Two separate DbContexts: one for business data, one for Identity (separation of concerns)
- Stub services for email/notifications/storage allow development without external dependencies
- Background hosted service (`LatePaymentHostedService`) runs periodic late payment checks
- Dapper is used alongside EF Core for performance-critical reporting queries

---

### 4. `LoanSuperMarket.Api` — The HTTP Shell

**References:** `LoanSuperMarket.Application`, `LoanSuperMarket.Infrastructure`, `LoanSuperMarket.Shared`
**Target:** `net10.0` (Web SDK)
**Key NuGet:** JWT Bearer Auth, Swashbuckle (Swagger), API Versioning, Health Checks

The API is intentionally thin. Controllers receive HTTP requests, dispatch them to MediatR, and return the result. No business logic lives here.

```
LoanSuperMarket.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── BorrowersController.cs
│   ├── LendersController.cs
│   ├── LoanProductsController.cs
│   ├── LoanApplicationsController.cs
│   ├── FundingController.cs
│   ├── PaymentsController.cs
│   ├── DashboardController.cs
│   ├── ReviewQueueController.cs
│   ├── WizardController.cs
│   ├── AuditLogsController.cs
│   ├── CreditController.cs
│   ├── NotificationsController.cs
│   ├── RoleController.cs
│   ├── SessionController.cs
│   ├── UserManagementController.cs
│   └── VettingController.cs
├── Hubs/
│   └── LoanHub.cs               ← SignalR hub for real-time loan status updates
├── Middleware/
│   ├── CorrelationIdMiddleware.cs   ← Adds correlation ID to every request
│   └── GlobalExceptionMiddleware.cs ← Catches unhandled exceptions, returns structured errors
├── Services/
│   └── SignalRNotifier.cs        ← Implements IRealTimeNotifier using SignalR
├── Program.cs                    ← Composition root (wires everything together)
├── appsettings.json
└── appsettings.Development.json
```

**Key design decisions:**
- Controllers are grouped by feature/domain area
- Global exception middleware ensures consistent error responses
- Correlation IDs enable distributed tracing
- Rate limiting protects against abuse (100 requests/minute per IP)
- Health check endpoint at `/health` monitors database connectivity

---

### 5. `LoanSuperMarket.Shared` — Data Contracts

**References:** None
**Target:** `net10.0`

This project contains DTOs (Data Transfer Objects) shared between the API and Blazor frontend. It has **zero business logic** — only data shapes.

```
LoanSuperMarket.Shared/
├── Auth/
│   ├── LoginRequest.cs
│   ├── RegisterRequest.cs
│   ├── AuthTokenResponse.cs
│   ├── ForgotPasswordRequest.cs
│   └── ResetPasswordRequest.cs
├── Borrowers/
│   ├── BorrowerDto.cs
│   └── CreateBorrowerRequest.cs
├── Common/
│   ├── ApiResponse.cs           ← Standard wrapper for all API responses
│   └── PagedResult.cs           ← Generic pagination wrapper
├── Configuration/
│   ├── JwtSettings.cs
│   ├── AccountSettings.cs
│   ├── RepaymentSettings.cs
│   └── NotificationPreferencesDto.cs
├── Dashboard/
│   ├── DashboardSummaryDto.cs
│   ├── LenderPortfolioDto.cs
│   └── ... (13 dashboard DTOs)
├── Funding/
│   ├── FundingQueueItemDto.cs
│   ├── AcceptFundingRequest.cs
│   └── ... (8 funding DTOs)
├── Grids/
│   ├── GridQueryRequest.cs      ← Standardized paging/sorting/filtering
│   └── SortDirection.cs
├── Lenders/
├── LoanApplications/
├── LoanProducts/
├── Payments/
├── Roles/
└── Users/
```

**Why a separate Shared project?**
- The Blazor frontend needs to know the shape of API requests/responses
- But Blazor should NOT reference Application or Domain (it would pull in server-side dependencies)
- Shared gives both sides a common contract without coupling them

---

### 6. `LoanSuperMarket.Blazor` — The Frontend

**References:** `LoanSuperMarket.Shared` only
**Target:** `net10.0` (Blazor WebAssembly SDK)
**Key NuGet:** Blazor WebAssembly, SignalR Client, Components Authorization

The Blazor WebAssembly app runs entirely in the browser. It communicates with the API via HTTP and SignalR.

```
LoanSuperMarket.Blazor/
├── Components/
│   ├── Audit/
│   ├── Borrowers/
│   ├── Common/              ← Reusable UI components (buttons, cards, etc.)
│   ├── Dashboard/
│   ├── DataGrid/            ← Generic data grid component
│   ├── Drawers/
│   ├── Forms/
│   ├── Funding/
│   ├── Lenders/
│   ├── LoanApplications/
│   ├── LoanProducts/
│   ├── Modals/
│   ├── Notifications/
│   └── Payments/
├── Layout/
│   └── MainLayout.razor     ← App shell (sidebar, header, content area)
├── Pages/
│   ├── Auth/                ← Login, Register, Forgot Password
│   ├── Admin/               ← Admin-only pages
│   ├── Account/             ← User profile, settings
│   ├── Home.razor
│   ├── BorrowerDashboard.razor
│   ├── LenderDashboard.razor
│   ├── LoanProducts.razor
│   ├── LoanApplications.razor
│   ├── LoanApplicationWizard.razor
│   ├── Payments.razor
│   ├── FundingQueue.razor
│   └── ... (22 page files)
├── Services/
│   ├── ApiClients/          ← Typed HTTP clients for each API area
│   ├── Auth/                ← JWT state provider, token handler
│   ├── DataGrid/            ← Grid state management
│   ├── Drawers/
│   ├── Modals/
│   ├── Notifications/
│   ├── LoanHubClient.cs    ← SignalR client for real-time updates
│   ├── ThemeService.cs     ← Dark/light mode toggle
│   └── WizardStateService.cs ← Multi-step form state
├── wwwroot/
│   ├── css/                 ← Tailwind CSS output
│   ├── index.html           ← SPA entry point
│   └── appsettings.json     ← Client-side config (ApiBaseUrl)
├── App.razor                ← Root component with routing
├── Program.cs               ← Service registration for WASM
├── tailwind.config.js       ← Tailwind + DaisyUI configuration
└── package.json             ← Node dependencies (Tailwind build)
```

**Key design decisions:**
- API clients are typed services (one per domain area) — not raw `HttpClient` calls scattered in components
- `AuthTokenHandler` automatically attaches JWT tokens and handles 401 refresh
- SignalR client provides real-time loan status updates without polling
- Tailwind CSS with DaisyUI provides consistent, accessible styling

---

## Dependency Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         SOLUTION DEPENDENCY MAP                        │
└──────────────────────────────────────────────────────────────────────┘

  ┌─────────────────┐         ┌─────────────────────┐
  │  LoanSuperMarket│         │   LoanSuperMarket   │
  │     .Blazor     │────────▶│      .Shared         │◀─────────────┐
  └─────────────────┘         └─────────────────────┘              │
                                        ▲                           │
                                        │                           │
  ┌─────────────────┐                   │                           │
  │  LoanSuperMarket│───────────────────┘                           │
  │      .Api       │                                               │
  │                 │──────┐                                        │
  └─────────────────┘      │                                        │
         │                 │                                        │
         │                 ▼                                        │
         │    ┌─────────────────────────┐                           │
         │    │    LoanSuperMarket      │                           │
         │    │    .Infrastructure      │                           │
         │    └─────────────────────────┘                           │
         │                 │                                        │
         │                 │                                        │
         ▼                 ▼                                        │
  ┌─────────────────────────────────┐                               │
  │      LoanSuperMarket            │                               │
  │      .Application               │───────────────────────────────┘
  └─────────────────────────────────┘
                   │
                   ▼
  ┌─────────────────────────────────┐
  │      LoanSuperMarket            │
  │         .Domain                 │
  └─────────────────────────────────┘
```

**Simplified text version:**

```
Blazor ──────────────────────────────────▶ Shared
Api ─────────────────────────────────────▶ Shared
Api ─────▶ Application ─────▶ Domain
Api ─────▶ Infrastructure ──▶ Application ──▶ Domain
                             └──▶ Domain (direct)
Application ─────────────────────────────▶ Shared
```

**What this means in practice:**
- `Domain` has ZERO project references (it's the innermost layer)
- `Application` references only `Domain` and `Shared`
- `Infrastructure` references `Application` and `Domain`
- `Api` references `Application`, `Infrastructure`, and `Shared`
- `Blazor` references ONLY `Shared` (completely decoupled from server-side logic)

---

## Key Files with Code Snippets

### 1. Application Layer — `DependencyInjection.cs`

This file registers all Application-layer services. It's called from `Program.cs` via `builder.Services.AddApplication()`.

```csharp
// src/LoanSuperMarket.Application/DependencyInjection.cs

using FluentValidation;
using LoanSuperMarket.Application.Common.Behaviours;
using LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LoanSuperMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Auto-register all MediatR handlers (Commands, Queries) from this assembly
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        // Auto-register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline Behaviours — these run in order for EVERY MediatR request
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccountStatusBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LimitEnforcementBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceAuthorizationBehaviour<,>));

        services.AddScoped<ProductMatchingService>();

        return services;
    }
}
```

**What's happening:**
- `AddMediatR` scans the assembly and registers every `IRequestHandler<,>` automatically
- `AddValidatorsFromAssembly` finds every `AbstractValidator<T>` and registers it
- Pipeline behaviours are middleware that wrap every command/query (logging → performance → validation → caching → account check → limits → authorization → actual handler)

---

### 2. Infrastructure Layer — `DependencyInjection.cs`

This file registers all concrete implementations. Called via `builder.Services.AddInfrastructure(builder.Configuration)`.

```csharp
// src/LoanSuperMarket.Infrastructure/DependencyInjection.cs

using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Funding;
using LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;
using LoanSuperMarket.Application.Features.Payments.LateDetection;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Services;
using LoanSuperMarket.Infrastructure.Identity;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Infrastructure.Repositories;
using LoanSuperMarket.Infrastructure.Services;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanSuperMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // ── Database Contexts ──────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContext<AuthIdentityDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // ── ASP.NET Identity ───────────────────────────────────────────────
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<CustomRole>()
        .AddEntityFrameworkStores<AuthIdentityDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager();

        // ── Core Services ──────────────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();

        // ── Repositories (interface → implementation) ──────────────────────
        services.AddScoped<ILoanProductRepository, LoanProductRepository>();
        services.AddScoped<IBorrowerRepository, BorrowerRepository>();
        services.AddScoped<ILenderRepository, LenderRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IApplicationDocumentRepository, ApplicationDocumentRepository>();

        // ── Identity & Security Services ───────────────────────────────────
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IClientInfoProvider, ClientInfoProvider>();

        // ── External Service Stubs (swap for real implementations later) ───
        services.AddScoped<IEmailService, NoOpEmailService>();
        services.AddScoped<IDocumentStorageService, StubDocumentStorageService>();
        services.AddScoped<INotificationService, StubNotificationService>();

        // ── Query Services ─────────────────────────────────────────────────
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IRoleQueryService, RoleQueryService>();
        services.AddScoped<IPlatformReportService, DapperPlatformReportService>();

        // ── Funding & Payment Engine ───────────────────────────────────────
        services.AddScoped<IAmortizationService, AmortizationService>();
        services.AddScoped<IPaymentProcessor, PaymentProcessor>();
        services.AddScoped<ProductMatchingService>();
        services.AddScoped<LatePaymentService>();

        // ── Configuration Binding ──────────────────────────────────────────
        services.Configure<RepaymentSettings>(
            configuration.GetSection("RepaymentSettings"));

        // ── Background Services ────────────────────────────────────────────
        services.AddHostedService<LatePaymentHostedService>();

        return services;
    }
}
```

**What's happening:**
- Every `IXxxRepository` interface (defined in Application) gets mapped to its concrete `XxxRepository` class
- Stub services (`NoOpEmailService`, `StubNotificationService`) allow development without external dependencies — swap them for real implementations when ready
- Configuration is bound from `appsettings.json` using the Options pattern
- A background hosted service runs periodic tasks (late payment detection)

---

### 3. API Layer — `Program.cs` (Composition Root)

This is where everything comes together. The API's `Program.cs` is the **composition root** — it configures the DI container, middleware pipeline, and starts the application.

```csharp
// src/LoanSuperMarket.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AccountSettings>(
    builder.Configuration.GetSection(AccountSettings.SectionName));

// ── CORS ───────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("CorsSettings:AllowedOrigins")
    .Get<string[]>() ?? ["https://localhost:5036", "http://localhost:5036"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();  // Required for SignalR
    });
});

// ── JWT Authentication ─────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* token validation parameters */ });

// ── Authorization Policies ─────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.Configure(options);
});

// ── Framework Services ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");
builder.Services.AddRateLimiter(/* 100 req/min per IP */);

// ── Clean Architecture DI ──────────────────────────────────────────────
// These two lines wire up the entire application:
builder.Services.AddApplication();                        // ← Application layer
builder.Services.AddInfrastructure(builder.Configuration); // ← Infrastructure layer

// API-specific service (not in Infrastructure because it depends on SignalR hub)
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

var app = builder.Build();

// ── Seed Data ──────────────────────────────────────────────────────────
await IdentitySeeder.SeedAsync(app.Services);
await DevelopmentDataSeeder.SeedAsync(app.Services);

// ── Middleware Pipeline (order matters!) ────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();   // 1. Add correlation ID
app.UseMiddleware<GlobalExceptionMiddleware>(); // 2. Catch all exceptions
app.UseHttpsRedirection();                     // 3. Force HTTPS
app.UseCors("BlazorCorsPolicy");               // 4. CORS headers
app.UseRateLimiter();                          // 5. Rate limiting
app.UseAuthentication();                       // 6. Who are you?
app.UseAuthorization();                        // 7. Are you allowed?
app.MapControllers();                          // 8. Route to controllers
app.MapHub<LoanHub>("/hubs/loans");            // 9. SignalR endpoint
app.MapHealthChecks("/health");                // 10. Health check

app.Run();
```

**Key takeaway:** The two lines `AddApplication()` and `AddInfrastructure(configuration)` are where Clean Architecture's DI magic happens. The API doesn't need to know about individual repositories or services — each layer registers its own dependencies.

---

### 4. Blazor Frontend — `Program.cs`

The Blazor app registers its own services independently. It only knows about `Shared` DTOs and its own API clients.

```csharp
// src/LoanSuperMarket.Blazor/Program.cs

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

// ── Authentication ─────────────────────────────────────────────────────
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// ── HTTP Client with automatic token attachment ────────────────────────
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

// ── API Clients (one per domain area) ──────────────────────────────────
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<LoanProductsApiClient>();
builder.Services.AddScoped<BorrowersApiClient>();
builder.Services.AddScoped<LendersApiClient>();
builder.Services.AddScoped<LoanApplicationsApiClient>();
builder.Services.AddScoped<WizardApiClient>();
builder.Services.AddScoped<ReviewQueueApiClient>();
builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<FundingApiClient>();
builder.Services.AddScoped<PaymentsApiClient>();

// ── Real-time & UI Services ────────────────────────────────────────────
builder.Services.AddScoped<LoanHubClient>();     // SignalR
builder.Services.AddScoped<ThemeService>();       // Dark/light mode
builder.Services.AddScoped<ToastService>();       // Toast notifications
builder.Services.AddScoped<ModalService>();       // Modal dialogs
builder.Services.AddScoped<DrawerService>();      // Slide-out panels
builder.Services.AddScoped<WizardStateService>(); // Multi-step form state

await builder.Build().RunAsync();
```

**Key takeaway:** Blazor has NO reference to Application, Domain, or Infrastructure. It only knows about `Shared` DTOs and communicates with the API via typed HTTP clients.

---

### 5. Example: A Complete CQRS Feature

Here's how a single feature (`CreateLoanProduct`) flows through the architecture:

**Command (Application layer):**
```csharp
// src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommand.cs

public sealed record CreateLoanProductCommand(
    string Title,
    string Description,
    decimal MinimumAmount,
    decimal MaximumAmount,
    decimal InterestRate,
    int MinimumTermMonths,
    int MaximumTermMonths,
    Guid LenderId) : IRequest<Guid>;
```

**Handler (Application layer):**
```csharp
// src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommandHandler.cs

public sealed class CreateLoanProductCommandHandler
    : IRequestHandler<CreateLoanProductCommand, Guid>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public CreateLoanProductCommandHandler(ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<Guid> Handle(
        CreateLoanProductCommand request,
        CancellationToken cancellationToken)
    {
        // Use Domain Value Objects to enforce business rules
        var minimumAmount = Money.Create(request.MinimumAmount);
        var maximumAmount = Money.Create(request.MaximumAmount);
        var interestRate = InterestRate.Create(request.InterestRate);

        // Use Domain Entity factory method
        var loanProduct = LoanProduct.Create(
            request.Title, request.Description,
            minimumAmount, maximumAmount, interestRate,
            request.MinimumTermMonths, request.MaximumTermMonths,
            request.LenderId);

        // Persist via interface (implementation is in Infrastructure)
        await _loanProductRepository.AddAsync(loanProduct, cancellationToken);
        await _loanProductRepository.SaveChangesAsync(cancellationToken);

        return loanProduct.Id;
    }
}
```

**Interface (Application layer):**
```csharp
// src/LoanSuperMarket.Application/Common/Interfaces/ILoanProductRepository.cs

public interface ILoanProductRepository
{
    Task AddAsync(LoanProduct loanProduct, CancellationToken cancellationToken);
    Task<LoanProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoanProduct>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LoanProduct>> GetPublishedAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PagedResult<LoanProductDto>> GetPagedAsync(
        GridQueryRequest request, CancellationToken cancellationToken);
}
```

**The flow:**
1. Controller receives HTTP POST → creates `CreateLoanProductCommand` → sends to MediatR
2. MediatR pipeline runs: Logging → Performance → Validation → Handler
3. Handler uses `ILoanProductRepository` (injected by DI)
4. At runtime, DI provides `LoanProductRepository` (from Infrastructure)
5. Repository uses EF Core to persist to SQL Server

---

### 6. Pipeline Behaviour Example — Validation

Every MediatR request passes through pipeline behaviours. Here's how validation works automatically:

```csharp
// src/LoanSuperMarket.Application/Common/Behaviours/ValidationBehaviour.cs

public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);  // No validators? Skip.

        var context = new ValidationContext<TRequest>(request);

        // Run ALL validators for this request type
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .Select(e => e.ErrorMessage)
            .Distinct()
            .ToList();

        if (errors.Count > 0)
            throw new ApplicationValidationException(errors);  // Short-circuit!

        return await next(cancellationToken);  // Validation passed → continue to handler
    }
}
```

**Why this matters:** You never need to manually validate in handlers. Just create a `CreateLoanProductCommandValidator` class and it runs automatically before the handler executes.

---

### 7. Domain Base Classes

```csharp
// src/LoanSuperMarket.Domain/Common/BaseEntity.cs

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
```

```csharp
// src/LoanSuperMarket.Domain/Common/AuditableEntity.cs

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

All business entities inherit from `AuditableEntity`, giving them automatic ID generation and audit timestamps.

---

## How to Add a New Feature

Let's say you need to add a "Close Loan" feature. Here's exactly where each piece goes:

### Step 1: Domain (if needed)

If the feature requires new business rules, entities, or enums:

```
src/LoanSuperMarket.Domain/Enums/LoanClosureReason.cs    ← New enum
src/LoanSuperMarket.Domain/Entities/LoanApplication.cs   ← Add Close() method
```

### Step 2: Shared DTOs

Define the request/response shapes that API and Blazor will share:

```
src/LoanSuperMarket.Shared/LoanApplications/CloseLoanRequest.cs
src/LoanSuperMarket.Shared/LoanApplications/CloseLoanResultDto.cs
```

### Step 3: Application Layer (Command + Handler + Validator)

Create a new feature folder:

```
src/LoanSuperMarket.Application/Features/LoanApplications/CloseLoan/
├── CloseLoanCommand.cs           ← implements IRequest<CloseLoanResultDto>
├── CloseLoanCommandHandler.cs    ← implements IRequestHandler<...>
└── CloseLoanCommandValidator.cs  ← implements AbstractValidator<CloseLoanCommand>
```

If you need a new interface (e.g., for a new external service), add it to:
```
src/LoanSuperMarket.Application/Common/Interfaces/ILoanClosureService.cs
```

### Step 4: Infrastructure (if new interface was added)

Implement the interface:
```
src/LoanSuperMarket.Infrastructure/Services/LoanClosureService.cs
```

Register it in `DependencyInjection.cs`:
```csharp
services.AddScoped<ILoanClosureService, LoanClosureService>();
```

### Step 5: API Controller

Add an endpoint to the existing controller (or create a new one):

```csharp
// src/LoanSuperMarket.Api/Controllers/LoanApplicationsController.cs

[HttpPost("{id}/close")]
[Authorize(Roles = "Admin,Lender")]
public async Task<ActionResult<CloseLoanResultDto>> CloseLoan(
    Guid id, CloseLoanRequest request)
{
    var command = new CloseLoanCommand(id, request.Reason);
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

### Step 6: Blazor Frontend

Add an API client method and a UI component:

```
src/LoanSuperMarket.Blazor/Services/ApiClients/LoanApplicationsApiClient.cs  ← Add CloseLoanAsync()
src/LoanSuperMarket.Blazor/Components/LoanApplications/CloseLoanModal.razor  ← UI component
```

### Step 7: Tests

```
tests/LoanSuperMarket.Application.Tests/Features/LoanApplications/CloseLoanCommandHandlerTests.cs
tests/LoanSuperMarket.Domain.Tests/Entities/LoanApplicationCloseTests.cs
```

---

## Key Rules

These are non-negotiable architectural rules. If you find yourself breaking one, stop and reconsider.

### 1. Domain NEVER references Infrastructure

```
❌ Domain → Infrastructure (NEVER)
❌ Domain → Application (NEVER)
✅ Domain → nothing (it's self-contained)
```

The Domain layer has zero knowledge of databases, HTTP, file systems, or any framework. If you need to call an external service from domain logic, define an interface in Application and inject it.

### 2. Application defines interfaces, Infrastructure implements them

```csharp
// Application layer DEFINES:
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

// Infrastructure layer IMPLEMENTS:
public class SendGridEmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // Actual SendGrid API call here
    }
}
```

This is the **Dependency Inversion Principle** in action. The Application layer depends on abstractions (interfaces), not concrete implementations.

### 3. API is just a thin HTTP layer over Application

Controllers should be short. Their job is:
1. Receive the HTTP request
2. Map it to a Command or Query
3. Send it to MediatR
4. Return the result

```csharp
// GOOD — thin controller
[HttpPost]
public async Task<ActionResult<Guid>> Create(CreateLoanProductRequest request)
{
    var command = new CreateLoanProductCommand(/* map from request */);
    var id = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetById), new { id }, id);
}

// BAD — business logic in controller
[HttpPost]
public async Task<ActionResult<Guid>> Create(CreateLoanProductRequest request)
{
    if (request.InterestRate > 30) return BadRequest("Rate too high");  // ❌ This belongs in a Validator
    var product = new LoanProduct { ... };                               // ❌ This belongs in a Handler
    _dbContext.LoanProducts.Add(product);                                // ❌ This belongs in a Repository
    await _dbContext.SaveChangesAsync();                                  // ❌ This belongs in a Repository
    return Ok(product.Id);
}
```

### 4. Shared has no logic, only data contracts

The Shared project contains:
- Request DTOs (what the client sends)
- Response DTOs (what the API returns)
- Configuration classes (strongly-typed settings)
- Common wrappers (`ApiResponse<T>`, `PagedResult<T>`)

It does NOT contain:
- Validation logic
- Business rules
- Service interfaces
- Entity classes

### 5. Blazor references ONLY Shared

The Blazor frontend is completely decoupled from server-side logic. It:
- Uses `Shared` DTOs to know the shape of API requests/responses
- Communicates exclusively via HTTP (typed API clients) and SignalR
- Has its own service layer for UI concerns (modals, toasts, themes)

### 6. Each feature is self-contained in its folder

```
Features/
└── LoanProducts/
    └── CreateLoanProduct/
        ├── CreateLoanProductCommand.cs        ← What to do
        ├── CreateLoanProductCommandHandler.cs ← How to do it
        └── CreateLoanProductCommandValidator.cs ← Is the request valid?
```

Don't scatter related files across multiple folders. Everything for "Create Loan Product" lives in one place.

### 7. Register new services in the correct `DependencyInjection.cs`

| Service Type | Register In |
|-------------|-------------|
| MediatR handlers | Auto-registered (Application DI scans assembly) |
| FluentValidation validators | Auto-registered (Application DI scans assembly) |
| Repository implementations | Infrastructure `DependencyInjection.cs` |
| External service implementations | Infrastructure `DependencyInjection.cs` |
| API-specific services (e.g., SignalR notifier) | `Program.cs` |
| Blazor services | Blazor `Program.cs` |

---

## Quick Reference: "Where does this go?"

| I need to... | Put it in... |
|-------------|-------------|
| Add a new entity | `Domain/Entities/` |
| Add a new enum | `Domain/Enums/` |
| Add a value object | `Domain/ValueObjects/` |
| Add a new use case | `Application/Features/{Area}/{UseCaseName}/` |
| Define a new interface | `Application/Common/Interfaces/` |
| Implement a repository | `Infrastructure/Repositories/` |
| Implement an external service | `Infrastructure/Services/` |
| Add a new API endpoint | `Api/Controllers/` |
| Add a shared DTO | `Shared/{Area}/` |
| Add a Blazor page | `Blazor/Pages/` |
| Add a Blazor component | `Blazor/Components/{Area}/` |
| Add an API client method | `Blazor/Services/ApiClients/` |
| Add a database migration | `Infrastructure/Migrations/` |
| Add EF Core configuration | `Infrastructure/Persistence/Configurations/` |
| Add a unit test | `tests/LoanSuperMarket.{Layer}.Tests/` |

---

## Further Reading

- [docs/03-architecture-overview.md](../docs/03-architecture-overview.md) — High-level architecture diagrams
- [docs/04-domain-layer-deep-dive.md](../docs/04-domain-layer-deep-dive.md) — Detailed domain modeling
- [docs/05-application-layer-deep-dive.md](../docs/05-application-layer-deep-dive.md) — CQRS patterns in depth
- [docs/11-design-patterns-explained.md](../docs/11-design-patterns-explained.md) — Design patterns used in this project
