# CQRS with MediatR — Complete Guide

> **Audience:** C# developers who know ASP.NET Core but haven't used MediatR or CQRS before.
> After reading this document, you'll be able to create new commands, queries, handlers, validators, and understand how every request flows through the pipeline.

---

## Table of Contents

1. [What is CQRS?](#1-what-is-cqrs)
2. [Commands](#2-commands)
3. [Queries](#3-queries)
4. [Handlers](#4-handlers)
5. [Pipeline Behaviours (the Middleware)](#5-pipeline-behaviours-the-middleware)
6. [The Request Pipeline Flow](#6-the-request-pipeline-flow)
7. [How to Add a New Command/Query](#7-how-to-add-a-new-commandquery)
8. [ApiResponse\<T\> Wrapper](#8-apiresponset-wrapper)

---

## 1. What is CQRS?

### The Core Idea

**Command Query Responsibility Segregation (CQRS)** is a pattern that separates the operations that read data (Queries) from the operations that change data (Commands) into distinct models.

Think of it like a restaurant:
- **Commands** = the kitchen (changes state — cooks food, uses ingredients)
- **Queries** = the menu/display window (reads state — shows what's available)

### Why Separate Reads from Writes?

| Concern | Without CQRS | With CQRS |
|---------|-------------|-----------|
| Validation | Mixed in service methods | Dedicated per-command validators |
| Caching | Hard to know what's safe to cache | Queries are naturally cacheable |
| Authorization | One-size-fits-all | Different rules for reads vs writes |
| Scaling | Single model bottleneck | Read/write sides scale independently |
| Testing | Large service classes | Small, focused handler classes |

### How MediatR Implements This Pattern

MediatR is an **in-process mediator** — it decouples the *sender* of a request from the *handler* of that request. Instead of a controller calling a service directly, it sends a message through MediatR:

```
Controller  →  MediatR.Send(request)  →  Handler
```

MediatR provides:
1. **`IRequest<TResponse>`** — marker interface for commands/queries
2. **`IRequestHandler<TRequest, TResponse>`** — the handler that processes the request
3. **`IPipelineBehavior<TRequest, TResponse>`** — middleware that runs before/after every handler
4. **`ISender`** — the interface controllers use to dispatch requests

The controller never knows *which* handler will process the request. MediatR resolves it via DI.

---

## 2. Commands

### What is a Command?

A **Command** is a request that **changes state**. It tells the system to *do something*:
- Create a loan product
- Submit a loan application
- Approve a loan
- Archive a product

Commands may return a value (like the ID of the created entity) or return nothing.

### Convention in This Project

```csharp
public sealed record XxxCommand(...) : IRequest<TResponse>;
```

- `sealed record` — immutable, value-equality, concise syntax
- Parameters are positional record properties
- Implements `IRequest<TResponse>` where `TResponse` is what the handler returns

### Full Example: `CreateLoanProductCommand`

**File:** `src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommand.cs`

```csharp
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

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

### Parameter Breakdown

| Parameter | Type | Purpose |
|-----------|------|---------|
| `Title` | `string` | Display name of the loan product |
| `Description` | `string` | Detailed description shown to borrowers |
| `MinimumAmount` | `decimal` | Smallest loan amount allowed |
| `MaximumAmount` | `decimal` | Largest loan amount allowed |
| `InterestRate` | `decimal` | Annual interest rate percentage |
| `MinimumTermMonths` | `int` | Shortest repayment period |
| `MaximumTermMonths` | `int` | Longest repayment period |
| `LenderId` | `Guid` | The lender who owns this product |

**Return type:** `Guid` — the ID of the newly created loan product.

> **Why `sealed record`?** Records give you immutability (commands shouldn't be modified after creation), value equality (useful for testing), and a compact syntax. `sealed` prevents inheritance — each command is a self-contained message.

---

## 3. Queries

### What is a Query?

A **Query** is a request that **reads data** without changing anything. It asks the system a question:
- Get all loan products
- Get a borrower's active loans
- Get dashboard statistics

Queries always return data and should have **no side effects**.

### Convention in This Project

```csharp
public sealed record XxxQuery(...) : IRequest<TResponse>;
```

Same structure as commands, but semantically different — queries don't mutate state.

### Full Example: `GetLoanProductsQuery`

**File:** `src/LoanSuperMarket.Application/Features/LoanProducts/GetLoanProducts/GetLoanProductsQuery.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;

public sealed record GetLoanProductsQuery : IRequest<IReadOnlyList<LoanProductDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}
```

### Key Points

- **Return type:** `IReadOnlyList<LoanProductDto>` — a list of DTOs (not domain entities!)
- **`IResourceFilteredQuery`** — this marker interface tells the `ResourceAuthorizationBehaviour` to automatically set `FilterByUserId` and `FilterByRole` based on the current user. The handler can then use these to scope its database query.
- The query has **no constructor parameters** because it retrieves *all* products (filtered by the pipeline behaviour based on user role).

### The `IResourceFilteredQuery` Interface

```csharp
namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IResourceFilteredQuery
{
    /// <summary>
    /// The user ID to filter resources by. Set by the authorization behaviour
    /// for Borrower-only or Lender-only users. Null means no user-level filter (admin access).
    /// </summary>
    string? FilterByUserId { get; set; }

    /// <summary>
    /// The role context for filtering. "Borrower" or "Lender" indicates the type of
    /// ownership filter to apply. Null means no role-based filter (admin access).
    /// </summary>
    string? FilterByRole { get; set; }
}
```

---

## 4. Handlers

### What is a Handler?

A **Handler** contains the **business logic** for processing a single command or query. It's the "do the work" class. Each handler:
- Implements `IRequestHandler<TRequest, TResponse>`
- Receives dependencies via constructor injection
- Has exactly one public method: `Handle(...)`

### Convention in This Project

```csharp
public sealed class XxxHandler : IRequestHandler<TRequest, TResponse>
{
    // Constructor-injected dependencies (repositories, services)
    
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        // Business logic here
    }
}
```

### Full Example: `CreateLoanProductCommandHandler`

**File:** `src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommandHandler.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.ValueObjects;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

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
        var minimumAmount = Money.Create(request.MinimumAmount);
        var maximumAmount = Money.Create(request.MaximumAmount);
        var interestRate = InterestRate.Create(request.InterestRate);

        var loanProduct = LoanProduct.Create(
            request.Title,
            request.Description,
            minimumAmount,
            maximumAmount,
            interestRate,
            request.MinimumTermMonths,
            request.MaximumTermMonths,
            request.LenderId);

        await _loanProductRepository.AddAsync(loanProduct, cancellationToken);
        await _loanProductRepository.SaveChangesAsync(cancellationToken);

        return loanProduct.Id;
    }
}
```

### Line-by-Line Explanation

| Line(s) | What It Does |
|---------|-------------|
| `IRequestHandler<CreateLoanProductCommand, Guid>` | Tells MediatR "I handle `CreateLoanProductCommand` and return a `Guid`" |
| `private readonly ILoanProductRepository` | Repository interface injected via DI — the handler never knows about EF Core or SQL |
| `Money.Create(request.MinimumAmount)` | Creates a domain Value Object — validates the amount is positive, sets currency |
| `InterestRate.Create(request.InterestRate)` | Creates a domain Value Object — validates rate is between 0-100 |
| `LoanProduct.Create(...)` | Factory method on the domain entity — enforces all business rules at creation |
| `_loanProductRepository.AddAsync(...)` | Persists the new entity to the database |
| `_loanProductRepository.SaveChangesAsync(...)` | Commits the Unit of Work (EF Core SaveChanges) |
| `return loanProduct.Id` | Returns the generated ID back through the pipeline to the controller |

### How DI Injects Repository Interfaces

The handler depends on `ILoanProductRepository` (an interface defined in the Application layer). The actual implementation lives in the Infrastructure layer and is registered in DI:

```csharp
// In Infrastructure/DependencyInjection.cs
services.AddScoped<ILoanProductRepository, LoanProductRepository>();
```

This means:
- The **Application layer** defines *what* it needs (interface)
- The **Infrastructure layer** provides *how* it's done (implementation)
- The handler is **testable** — you can mock `ILoanProductRepository` in unit tests

### The Repository Interface

```csharp
namespace LoanSuperMarket.Application.Common.Interfaces;

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

### Full Example: `GetLoanProductsQueryHandler`

**File:** `src/LoanSuperMarket.Application/Features/LoanProducts/GetLoanProducts/GetLoanProductsQueryHandler.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;

public sealed class GetLoanProductsQueryHandler
    : IRequestHandler<GetLoanProductsQuery, IReadOnlyList<LoanProductDto>>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public GetLoanProductsQueryHandler(ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<IReadOnlyList<LoanProductDto>> Handle(
        GetLoanProductsQuery request,
        CancellationToken cancellationToken)
    {
        var loanProducts = await _loanProductRepository.GetAllAsync(cancellationToken);

        return loanProducts
            .Select(x => new LoanProductDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                MinimumAmount = x.MinimumAmount.Amount,
                MaximumAmount = x.MaximumAmount.Amount,
                Currency = x.MinimumAmount.Currency,
                InterestRate = x.InterestRate.Percentage,
                MinimumTermMonths = x.MinimumTermMonths,
                MaximumTermMonths = x.MaximumTermMonths,
                LenderId = x.LenderId,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}
```

**Key differences from the command handler:**
- Returns a **list of DTOs** (not domain entities) — the query maps domain objects to a flat DTO
- **No state changes** — just reads from the repository and transforms
- The `request.FilterByUserId` and `request.FilterByRole` properties were already set by the `ResourceAuthorizationBehaviour` before this handler executes

---

## 5. Pipeline Behaviours (the Middleware)

### What Are Pipeline Behaviours?

Pipeline behaviours are **cross-cutting concerns** that execute before and/or after every handler — like ASP.NET middleware, but for MediatR requests. They form a chain:

```
Request → Behaviour 1 → Behaviour 2 → ... → Handler → Response
```

Each behaviour can:
- **Inspect** the request before the handler runs
- **Short-circuit** the pipeline (return early without calling the handler)
- **Modify** the response after the handler runs
- **Throw exceptions** to reject the request

### The Execution Order

Behaviours run in the order they're registered in `DependencyInjection.cs`:

```
1. LoggingBehaviour          — logs request start/end
2. PerformanceBehaviour      — warns if request takes > 500ms
3. ValidationBehaviour       — runs FluentValidation rules
4. CachingBehaviour          — returns cached response if available
5. AccountStatusBehaviour    — blocks suspended/closed accounts
6. LimitEnforcementBehaviour — enforces credit/capital limits
7. ResourceAuthorizationBehaviour — scopes data by user role
8. ═══ HANDLER ═══           — your business logic executes here
```

### Registration in `DependencyInjection.cs`

**File:** `src/LoanSuperMarket.Application/DependencyInjection.cs`

```csharp
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

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

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

**Key points:**
- `RegisterServicesFromAssembly` auto-discovers all `IRequestHandler<,>` implementations
- `AddValidatorsFromAssembly` auto-discovers all `AbstractValidator<T>` implementations
- Behaviours are registered as **open generics** (`typeof(IPipelineBehavior<,>)`) — they apply to ALL requests
- **Order matters!** The registration order determines execution order

---

### Behaviour 1: `LoggingBehaviour`

**Purpose:** Logs the start and end of every request for observability.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/LoggingBehaviour.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling request {RequestName}", requestName);

        var response = await next(cancellationToken);

        _logger.LogInformation("Handled request {RequestName}", requestName);

        return response;
    }
}
```

**How it works:**
1. Logs "Handling request CreateLoanProductCommand"
2. Calls `next(cancellationToken)` — this passes control to the next behaviour in the chain
3. When the handler (and all subsequent behaviours) complete, logs "Handled request CreateLoanProductCommand"
4. Returns the response unchanged

**The `next` delegate** is the key concept — calling it invokes the rest of the pipeline. If you don't call `next`, the handler never executes (short-circuit).

---

### Behaviour 2: `PerformanceBehaviour`

**Purpose:** Detects slow requests and logs a warning if any request takes longer than 500ms.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/PerformanceBehaviour.cs`

```csharp
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class PerformanceBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        var requestName = typeof(TRequest).Name;

        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Long running request {RequestName} took {ElapsedMilliseconds} ms",
                requestName,
                elapsedMilliseconds);
        }

        return response;
    }
}
```

**How it works:**
1. Starts a stopwatch before calling `next`
2. Lets the entire rest of the pipeline execute (including the handler)
3. Checks elapsed time — if > 500ms, logs a warning
4. This helps identify performance bottlenecks in production

---

### Behaviour 3: `ValidationBehaviour`

**Purpose:** Runs all FluentValidation validators for the request. If any validation fails, throws an exception *before* the handler executes.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/ValidationBehaviour.cs`

```csharp
using FluentValidation;
using LoanSuperMarket.Application.Common.Models;
using MediatR;

namespace LoanSuperMarket.Application.Common.Behaviours;

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
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToList();

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }

        return await next(cancellationToken);
    }
}
```

**How it works:**
1. DI injects **all** validators registered for `TRequest` (there can be zero or many)
2. If no validators exist for this request type, skips straight to `next`
3. Runs all validators **in parallel** using `Task.WhenAll`
4. Collects all error messages, deduplicates them
5. If there are errors, throws `ApplicationValidationException` — the handler **never executes**
6. If validation passes, calls `next` to continue the pipeline

**Example validator that pairs with `CreateLoanProductCommand`:**

```csharp
using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed class CreateLoanProductCommandValidator : AbstractValidator<CreateLoanProductCommand>
{
    public CreateLoanProductCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.MinimumAmount)
            .GreaterThan(0);

        RuleFor(x => x.MaximumAmount)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumAmount);

        RuleFor(x => x.InterestRate)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.MinimumTermMonths)
            .GreaterThan(0);

        RuleFor(x => x.MaximumTermMonths)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumTermMonths);

        RuleFor(x => x.LenderId)
            .NotEmpty();
    }
}
```

The validator is **auto-discovered** by `AddValidatorsFromAssembly` — you just create the class and it's automatically wired into the pipeline.

---

### Behaviour 4: `CachingBehaviour`

**Purpose:** Implements the **cache-aside pattern** for queries. If a cached response exists, returns it immediately without hitting the database.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/CachingBehaviour.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class CachingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> _logger;

    public CachingBehaviour(IMemoryCache cache, ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cacheKey = cacheableQuery.CacheKey;

        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        var response = await next(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheableQuery.CacheMinutes)
        };

        _cache.Set(cacheKey, response, cacheOptions);

        return response;
    }
}

public interface ICacheableQuery
{
    string CacheKey { get; }
    int CacheMinutes => 5; // Default: 5 minutes
}
```

**How it works:**
1. Checks if the request implements `ICacheableQuery` — if not, skips to `next`
2. Looks up the `CacheKey` in the in-memory cache
3. **Cache HIT:** Returns the cached response immediately — handler never executes
4. **Cache MISS:** Calls `next` to execute the handler, then stores the response in cache
5. Cache entries expire after `CacheMinutes` (default 5 minutes)

**To make a query cacheable**, implement `ICacheableQuery`:

```csharp
public sealed record GetDashboardStatsQuery(Guid UserId) 
    : IRequest<DashboardStatsDto>, ICacheableQuery
{
    public string CacheKey => $"dashboard-stats-{UserId}";
    public int CacheMinutes => 2;
}
```

---

### Behaviour 5: `AccountStatusBehaviour`

**Purpose:** Enforces account status restrictions. Suspended/closed accounts are blocked entirely. Accounts on hold or pending approval have limited access.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/AccountStatusBehaviour.cs`

```csharp
using LoanSuperMarket.Application.Common.Exceptions;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class AccountStatusBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public AccountStatusBehaviour(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip enforcement for unauthenticated requests (e.g., login, register)
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await next(cancellationToken);
        }

        var user = await _identityService.GetUserByIdAsync(
            _currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            return await next(cancellationToken);
        }

        switch (user.AccountStatus)
        {
            case AccountStatus.Closed:
                throw new AccountStatusException(
                    AccountStatus.Closed,
                    "AUTH_ACCOUNT_CLOSED",
                    "This account has been permanently closed. All access is denied.");

            case AccountStatus.Suspended:
                throw new AccountStatusException(
                    AccountStatus.Suspended,
                    "AUTH_ACCOUNT_SUSPENDED",
                    "This account has been suspended. All platform access is denied.");

            case AccountStatus.PendingApproval:
                EnforcePendingApproval(request);
                break;

            case AccountStatus.Hold:
                EnforceHold(request);
                break;

            case AccountStatus.Blocked:
                EnforceBlocked(request, user.BlockedActivity);
                break;

            case AccountStatus.Active:
            case AccountStatus.DocumentsRequested:
                break; // No restrictions
        }

        return await next(cancellationToken);
    }

    private static void EnforcePendingApproval(TRequest request)
    {
        if (request is IAllowPendingApproval) return;
        throw new AccountStatusException(
            AccountStatus.PendingApproval,
            "AUTH_PENDING_APPROVAL",
            "Your account is pending approval.");
    }

    private static void EnforceHold(TRequest request)
    {
        if (request is ICreateLoanCommand)
            throw new AccountStatusException(AccountStatus.Hold, "AUTH_ACCOUNT_HOLD",
                "Your account is on hold. You cannot create new loan applications.");

        if (request is ICreateProductCommand)
            throw new AccountStatusException(AccountStatus.Hold, "AUTH_ACCOUNT_HOLD",
                "Your account is on hold. You cannot create new loan products.");
    }

    private static void EnforceBlocked(TRequest request, string? blockedActivity)
    {
        if (string.IsNullOrEmpty(blockedActivity)) return;

        var isBorrowingBlocked = blockedActivity.Equals("Borrowing", StringComparison.OrdinalIgnoreCase)
                                 || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);
        var isLendingBlocked = blockedActivity.Equals("Lending", StringComparison.OrdinalIgnoreCase)
                               || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);

        if (isBorrowingBlocked && request is ICreateLoanCommand)
            throw new AccountStatusException(AccountStatus.Blocked, "AUTH_ACCOUNT_BLOCKED",
                "Your account is blocked from borrowing activities.");

        if (isLendingBlocked && request is ICreateProductCommand)
            throw new AccountStatusException(AccountStatus.Blocked, "AUTH_ACCOUNT_BLOCKED",
                "Your account is blocked from lending activities.");
    }
}
```

**How it works:**
1. Skips unauthenticated requests (login, register don't need account status checks)
2. Looks up the user's current account status from the identity service
3. Based on status, either allows, restricts, or blocks the request:
   - **Closed/Suspended** → throws immediately (total block)
   - **PendingApproval** → only allows requests marked with `IAllowPendingApproval`
   - **Hold** → blocks new loan/product creation but allows everything else
   - **Blocked** → blocks specific activities (Borrowing, Lending, or Both)
   - **Active/DocumentsRequested** → no restrictions

**Marker interfaces** like `ICreateLoanCommand`, `ICreateProductCommand`, and `IAllowPendingApproval` are how you tag commands for specific behaviour enforcement.

---

### Behaviour 6: `LimitEnforcementBehaviour`

**Purpose:** Enforces financial limits — credit limits for borrowers and capital limits for lenders.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/LimitEnforcementBehaviour.cs`

```csharp
using LoanSuperMarket.Application.Common.Exceptions;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class LimitEnforcementBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly AccountSettings _accountSettings;

    public LimitEnforcementBehaviour(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IBorrowerRepository borrowerRepository,
        ILoanApplicationRepository loanApplicationRepository,
        IOptions<AccountSettings> accountSettings)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _borrowerRepository = borrowerRepository;
        _loanApplicationRepository = loanApplicationRepository;
        _accountSettings = accountSettings.Value;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only enforce limits on loan application or loan funding commands
        if (request is not ILoanApplicationCommand && request is not ILoanFundingCommand)
        {
            return await next(cancellationToken);
        }

        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await next(cancellationToken);
        }

        var user = await _identityService.GetUserByIdAsync(
            _currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            return await next(cancellationToken);
        }

        if (request is ILoanApplicationCommand loanCommand
            && _currentUserService.IsInRole("Borrower"))
        {
            await EnforceBorrowerLimits(loanCommand, user.CreditLimit, cancellationToken);
        }

        if (request is ILoanFundingCommand fundingCommand
            && _currentUserService.IsInRole("Lender"))
        {
            EnforceLenderCapitalLimit(fundingCommand, user.CapitalLimit);
        }

        return await next(cancellationToken);
    }

    private async Task EnforceBorrowerLimits(
        ILoanApplicationCommand command,
        decimal? creditLimit,
        CancellationToken cancellationToken)
    {
        // Enforce credit limit
        if (creditLimit.HasValue && command.Amount > creditLimit.Value)
        {
            throw new LimitExceededException(
                "LIMIT_CREDIT_EXCEEDED",
                $"The requested loan amount exceeds your credit limit of {creditLimit.Value:C}.");
        }

        // Enforce maximum active loans per borrower
        var borrower = await _borrowerRepository.GetByUserIdAsync(
            _currentUserService.UserId!, cancellationToken);

        if (borrower is null) return;

        var activeLoansCount = await _loanApplicationRepository
            .CountActiveByBorrowerIdAsync(borrower.Id, cancellationToken);

        if (activeLoansCount >= _accountSettings.MaxActiveLoansPerBorrower)
        {
            throw new LimitExceededException(
                "LIMIT_MAX_LOANS",
                $"You have reached the maximum number of active loans " +
                $"({_accountSettings.MaxActiveLoansPerBorrower}).");
        }
    }

    private static void EnforceLenderCapitalLimit(
        ILoanFundingCommand command, decimal? capitalLimit)
    {
        if (capitalLimit.HasValue && command.Amount > capitalLimit.Value)
        {
            throw new LimitExceededException(
                "LIMIT_CAPITAL_EXCEEDED",
                $"The funding amount exceeds your capital limit of {capitalLimit.Value:C}.");
        }
    }
}
```

**How it works:**
1. Only activates for requests implementing `ILoanApplicationCommand` or `ILoanFundingCommand`
2. For **borrowers**: checks if the loan amount exceeds their credit limit AND if they've hit the max active loans
3. For **lenders**: checks if the funding amount exceeds their capital limit
4. Throws `LimitExceededException` if any limit is breached — handler never executes

---

### Behaviour 7: `ResourceAuthorizationBehaviour`

**Purpose:** Automatically scopes query data based on the user's role. Admins see everything; borrowers/lenders only see their own resources.

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/ResourceAuthorizationBehaviour.cs`

```csharp
using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class ResourceAuthorizationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;

    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "CrmManager",
        "Auditor"
    };

    public ResourceAuthorizationBehaviour(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IResourceFilteredQuery filteredQuery)
        {
            return await next(cancellationToken);
        }

        ApplyResourceFilter(filteredQuery);

        return await next(cancellationToken);
    }

    private void ApplyResourceFilter(IResourceFilteredQuery query)
    {
        if (!_currentUserService.IsAuthenticated) return;

        var roles = _currentUserService.Roles;

        // Admin-level roles see everything
        if (roles.Any(role => AdminRoles.Contains(role)))
        {
            query.FilterByUserId = null;
            query.FilterByRole = null;
            return;
        }

        // Borrowers see only their own resources
        if (roles.Any(r => r.Equals("Borrower", StringComparison.OrdinalIgnoreCase)))
        {
            query.FilterByUserId = _currentUserService.UserId;
            query.FilterByRole = "Borrower";
            return;
        }

        // Lenders see only their own resources
        if (roles.Any(r => r.Equals("Lender", StringComparison.OrdinalIgnoreCase)))
        {
            query.FilterByUserId = _currentUserService.UserId;
            query.FilterByRole = "Lender";
            return;
        }

        // Safe default: filter by user ID
        query.FilterByUserId = _currentUserService.UserId;
        query.FilterByRole = null;
    }
}
```

**How it works:**
1. Only activates for requests implementing `IResourceFilteredQuery`
2. Checks the current user's roles
3. **Admin/CrmManager/Auditor** → sets both filters to `null` (see all data)
4. **Borrower** → sets `FilterByUserId` to their ID and `FilterByRole` to "Borrower"
5. **Lender** → sets `FilterByUserId` to their ID and `FilterByRole` to "Lender"
6. The handler then uses these filter values to scope its database query

**This is powerful because:** The handler doesn't need to know about authorization. It just reads `request.FilterByUserId` and applies it to the query. The authorization logic is centralized in one place.

---

## 6. The Request Pipeline Flow

### Visual Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          HTTP REQUEST                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  CONTROLLER                                                                   │
│                                                                               │
│  var result = await _sender.Send(new CreateLoanProductCommand(...));          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  MediatR.Send()                                                               │
│  Resolves the handler and builds the pipeline chain                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. LoggingBehaviour                                                          │
│     → Logs "Handling request CreateLoanProductCommand"                        │
│     → Calls next()                                                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  2. PerformanceBehaviour                                                      │
│     → Starts stopwatch                                                        │
│     → Calls next()                                                            │
│     → Logs warning if > 500ms                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  3. ValidationBehaviour                                                       │
│     → Runs CreateLoanProductCommandValidator                                  │
│     → If errors: throws ApplicationValidationException (STOPS HERE)           │
│     → If valid: calls next()                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  4. CachingBehaviour                                                          │
│     → Not ICacheableQuery? Calls next()                                       │
│     → Is ICacheableQuery + cache hit? Returns cached response (STOPS HERE)    │
│     → Is ICacheableQuery + cache miss? Calls next(), caches result            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  5. AccountStatusBehaviour                                                    │
│     → Checks user's account status                                            │
│     → Suspended/Closed? Throws exception (STOPS HERE)                         │
│     → Active? Calls next()                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  6. LimitEnforcementBehaviour                                                 │
│     → Checks credit/capital limits                                            │
│     → Limit exceeded? Throws LimitExceededException (STOPS HERE)              │
│     → Within limits? Calls next()                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  7. ResourceAuthorizationBehaviour                                            │
│     → Sets FilterByUserId / FilterByRole on the request                       │
│     → Calls next()                                                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ═══════════════════════════ HANDLER ═══════════════════════════              │
│                                                                               │
│  CreateLoanProductCommandHandler.Handle()                                     │
│  → Creates domain entities                                                    │
│  → Persists to database                                                       │
│  → Returns Guid                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                        Response bubbles back up
                    (through each behaviour in reverse)
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  CONTROLLER                                                                   │
│                                                                               │
│  return CreatedAtAction(..., ApiResponse<Guid>.Ok(loanProductId, "..."));    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          HTTP RESPONSE                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Controller Example

Here's how the controller dispatches requests through MediatR:

```csharp
[ApiController]
[Route("api/loan-products")]
[Authorize(Policy = "CanManageProducts")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly ISender _sender;

    public LoanProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanProductDto>>>> GetLoanProducts(
        CancellationToken cancellationToken)
    {
        var loanProducts = await _sender.Send(new GetLoanProductsQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<LoanProductDto>>.Ok(
            loanProducts,
            "Loan products retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLoanProduct(
        [FromBody] CreateLoanProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLoanProductCommand(
            request.Title,
            request.Description,
            request.MinimumAmount,
            request.MaximumAmount,
            request.InterestRate,
            request.MinimumTermMonths,
            request.MaximumTermMonths,
            request.LenderId);

        var loanProductId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetLoanProducts),
            new { id = loanProductId },
            ApiResponse<Guid>.Ok(loanProductId, "Loan product created successfully."));
    }
}
```

**Key points:**
- The controller injects `ISender` (not `IMediator`) — it only needs to *send* requests
- `_sender.Send(...)` dispatches the request into the pipeline
- The controller doesn't know about validation, caching, or authorization — the pipeline handles all of that
- The controller wraps the result in `ApiResponse<T>` for consistent API responses

---

## 7. How to Add a New Command/Query

### Step-by-Step: Adding a New Command

Let's say you need to add a "Deactivate Borrower" command.

**Step 1: Create the feature folder**

```
src/LoanSuperMarket.Application/Features/Borrowers/DeactivateBorrower/
```

**Step 2: Create the Command record**

```csharp
// DeactivateBorrowerCommand.cs
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.DeactivateBorrower;

public sealed record DeactivateBorrowerCommand(Guid BorrowerId, string Reason) : IRequest<Unit>;
```

> Use `Unit` (from MediatR) when the command doesn't return a meaningful value.

**Step 3: Create the Handler**

```csharp
// DeactivateBorrowerCommandHandler.cs
using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.DeactivateBorrower;

public sealed class DeactivateBorrowerCommandHandler
    : IRequestHandler<DeactivateBorrowerCommand, Unit>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public DeactivateBorrowerCommandHandler(IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<Unit> Handle(
        DeactivateBorrowerCommand request,
        CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByIdAsync(request.BorrowerId, cancellationToken)
            ?? throw new DomainException("Borrower not found.");

        borrower.Deactivate(request.Reason);

        await _borrowerRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

**Step 4: Create the Validator (optional but recommended)**

```csharp
// DeactivateBorrowerCommandValidator.cs
using FluentValidation;

namespace LoanSuperMarket.Application.Features.Borrowers.DeactivateBorrower;

public sealed class DeactivateBorrowerCommandValidator 
    : AbstractValidator<DeactivateBorrowerCommand>
{
    public DeactivateBorrowerCommandValidator()
    {
        RuleFor(x => x.BorrowerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
```

**Step 5: Add the Controller endpoint**

```csharp
[HttpPost("{id:guid}/deactivate")]
public async Task<ActionResult<ApiResponse<string>>> Deactivate(
    Guid id,
    [FromBody] DeactivateRequest request,
    CancellationToken cancellationToken)
{
    await _sender.Send(
        new DeactivateBorrowerCommand(id, request.Reason),
        cancellationToken);

    return Ok(ApiResponse<string>.Ok("Borrower deactivated.", "Action completed."));
}
```

**That's it!** No registration needed — MediatR auto-discovers the handler, FluentValidation auto-discovers the validator.

---

### Step-by-Step: Adding a New Query

**Step 1: Create the feature folder**

```
src/LoanSuperMarket.Application/Features/Borrowers/GetBorrowerById/
```

**Step 2: Create the Query record**

```csharp
// GetBorrowerByIdQuery.cs
using LoanSuperMarket.Shared.Borrowers;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowerById;

public sealed record GetBorrowerByIdQuery(Guid BorrowerId) : IRequest<BorrowerDto?>;
```

**Step 3: Create the Handler**

```csharp
// GetBorrowerByIdQueryHandler.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Borrowers;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowerById;

public sealed class GetBorrowerByIdQueryHandler
    : IRequestHandler<GetBorrowerByIdQuery, BorrowerDto?>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public GetBorrowerByIdQueryHandler(IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<BorrowerDto?> Handle(
        GetBorrowerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByIdAsync(
            request.BorrowerId, cancellationToken);

        if (borrower is null) return null;

        return new BorrowerDto
        {
            Id = borrower.Id,
            Name = borrower.FullName,
            Email = borrower.Email
            // ... map other properties
        };
    }
}
```

---

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| Command | `{Verb}{Noun}Command` | `CreateLoanProductCommand` |
| Query | `Get{Noun}Query` or `Get{Noun}By{Filter}Query` | `GetLoanProductsQuery`, `GetBorrowerByIdQuery` |
| Handler | `{CommandOrQuery}Handler` | `CreateLoanProductCommandHandler` |
| Validator | `{CommandOrQuery}Validator` | `CreateLoanProductCommandValidator` |
| Folder | `Features/{Entity}/{ActionName}/` | `Features/LoanProducts/CreateLoanProduct/` |

### When to Use Command vs Query

| Use a **Command** when... | Use a **Query** when... |
|--------------------------|------------------------|
| Creating a new entity | Fetching a list of items |
| Updating existing data | Getting a single item by ID |
| Deleting/archiving | Searching or filtering |
| Triggering a workflow (approve, submit) | Calculating statistics |
| Any operation with side effects | Any read-only operation |

---

## 8. ApiResponse\<T\> Wrapper

### The Class

**File:** `src/LoanSuperMarket.Shared/Common/ApiResponse.cs`

```csharp
namespace LoanSuperMarket.Shared.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = [error]
        };
    }

    public static ApiResponse<T> Fail(List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors
        };
    }
}
```

### Success Response Shape

```json
{
  "success": true,
  "message": "Loan product created successfully.",
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "errors": []
}
```

### Failure Response Shape

```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": [
    "'Title' must not be empty.",
    "'Minimum Amount' must be greater than '0'."
  ]
}
```

### How Errors Propagate: Domain → Application → API → Client

```
┌──────────────────────────────────────────────────────────────────────┐
│ DOMAIN LAYER                                                          │
│                                                                        │
│ Money.Create(-100)  →  throws DomainException("Amount must be > 0")  │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼ (exception bubbles up)
┌──────────────────────────────────────────────────────────────────────┐
│ APPLICATION LAYER                                                      │
│                                                                        │
│ ValidationBehaviour  →  throws ApplicationValidationException         │
│ AccountStatusBehaviour  →  throws AccountStatusException              │
│ LimitEnforcementBehaviour  →  throws LimitExceededException           │
│ Handler  →  may throw DomainException from entity methods             │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼ (exception bubbles up)
┌──────────────────────────────────────────────────────────────────────┐
│ API LAYER — GlobalExceptionMiddleware                                  │
│                                                                        │
│ catch (ApplicationValidationException ex)                             │
│   → 400 Bad Request + ApiResponse.Fail(ex.Errors)                    │
│                                                                        │
│ catch (DomainException ex)                                            │
│   → 400 Bad Request + ApiResponse.Fail(ex.Message)                   │
│                                                                        │
│ catch (Exception ex)                                                  │
│   → 500 Internal Server Error + ApiResponse.Fail("Unexpected error") │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│ CLIENT (Blazor)                                                        │
│                                                                        │
│ var response = await Http.PostAsJsonAsync<ApiResponse<Guid>>(...);    │
│ if (!response.Success)                                                │
│     ShowErrors(response.Errors);                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### The `GlobalExceptionMiddleware`

```csharp
namespace LoanSuperMarket.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationValidationException exception)
        {
            context.Response.StatusCode = 400; // Bad Request
            var response = ApiResponse<object>.Fail(exception.Errors.ToList());
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = 400; // Bad Request
            var response = ApiResponse<object>.Fail(exception.Message);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
            context.Response.StatusCode = 500; // Internal Server Error
            var response = ApiResponse<object>.Fail(
                "An unexpected error occurred. Please try again later.");
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
```

**Key design decisions:**
- **Validation errors** (from FluentValidation) → 400 with a list of error messages
- **Domain errors** (business rule violations) → 400 with a single error message
- **Unexpected errors** → 500 with a generic message (details logged server-side, never exposed to client)
- The client always gets a consistent `ApiResponse<T>` shape — it can always check `response.Success` and read `response.Errors`

---

## Quick Reference Card

| I want to... | Create... | Implements... |
|-------------|-----------|---------------|
| Change state | `sealed record XxxCommand(...) : IRequest<T>` | — |
| Read data | `sealed record XxxQuery(...) : IRequest<T>` | — |
| Process a request | `sealed class XxxHandler : IRequestHandler<TReq, TRes>` | — |
| Validate input | `sealed class XxxValidator : AbstractValidator<TReq>` | Auto-discovered |
| Cache a query | Add `ICacheableQuery` to your query record | `CacheKey`, `CacheMinutes` |
| Scope by user role | Add `IResourceFilteredQuery` to your query record | `FilterByUserId`, `FilterByRole` |
| Allow during pending approval | Add `IAllowPendingApproval` to your command/query | Marker interface |
| Enforce borrower limits | Add `ILoanApplicationCommand` to your command | `Amount` property |
| Enforce lender limits | Add `ILoanFundingCommand` to your command | `Amount` property |

---

## Summary

The CQRS + MediatR architecture in this project gives you:

1. **Separation of concerns** — each handler does one thing
2. **Automatic cross-cutting** — validation, logging, caching, auth all happen without handler code
3. **Testability** — handlers are small, focused, and mockable
4. **Consistency** — every request flows through the same pipeline
5. **Extensibility** — add a new behaviour and it applies to ALL requests automatically

When you create a new feature, you're just adding a record (the message) and a class (the handler). The pipeline takes care of the rest.
