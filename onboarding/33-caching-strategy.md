# 33 — Caching Strategy

## Overview

The platform uses in-memory caching via a MediatR pipeline behaviour to cache query results. Queries that implement `ICacheableQuery` are automatically cached using the cache-aside pattern. This reduces database load for frequently-accessed, rarely-changing data like loan product listings and dashboard summaries.

---

## Feature Requirements (Plain English)

1. Cache query results in memory to reduce database round-trips.
2. Only cache queries that opt-in via a marker interface.
3. Each cacheable query defines its own cache key and TTL (time-to-live).
4. Cache is automatically bypassed for commands (mutations).
5. Cache entries are invalidated when related data changes.
6. Configurable cache duration per query type.
7. Cache hits/misses are logged for monitoring.

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Cache store | IMemoryCache | In-process memory |
| Pipeline | MediatR IPipelineBehavior | Decorator/interceptor |
| Opt-in | ICacheableQuery interface | Marker interface |
| Invalidation | Manual removal | Cache-aside with explicit invalidation |

---

## IMemoryCache Registration

```csharp
// API Program.cs
builder.Services.AddMemoryCache();
```

This registers `IMemoryCache` as a singleton. In-memory cache is per-process — in a multi-instance deployment, each instance has its own cache.

---

## ICacheableQuery Interface

```csharp
// src/LoanSuperMarket.Application/Common/Behaviours/CachingBehaviour.cs (bottom of file)

/// <summary>
/// Marker interface for queries that should be cached.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Unique cache key for this query instance.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long to cache the result (default: 5 minutes).
    /// </summary>
    int CacheMinutes => 5;
}
```

### Implementing ICacheableQuery

```csharp
// Example: Cache the loan products list for 10 minutes
public sealed record GetLoanProductsQuery
    : IRequest<IReadOnlyList<LoanProductDto>>, ICacheableQuery
{
    public string CacheKey => "loan-products-all";
    public int CacheMinutes => 10;
}

// Example: Cache with parameters in the key
public sealed record GetLoanProductByIdQuery(Guid Id)
    : IRequest<LoanProductDto?>, ICacheableQuery
{
    public string CacheKey => $"loan-product-{Id}";
    public int CacheMinutes => 5;
}

// Example: Cache dashboard summary for 2 minutes
public sealed record GetDashboardSummaryQuery
    : IRequest<DashboardSummaryDto>, ICacheableQuery
{
    public string CacheKey => "dashboard-summary";
    public int CacheMinutes => 2;
}
```

---

## CachingBehaviour — MediatR Pipeline

```csharp
// src/LoanSuperMarket.Application/Common/Behaviours/CachingBehaviour.cs
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that caches query results.
/// Only applies to requests that implement ICacheableQuery.
/// Uses cache-aside pattern with configurable expiration.
/// </summary>
public sealed class CachingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> _logger;

    public CachingBehaviour(
        IMemoryCache cache,
        ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache requests that implement ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cacheKey = cacheableQuery.CacheKey;

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse)
            && cachedResponse is not null)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        // Execute the handler
        var response = await next(cancellationToken);

        // Store in cache
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheableQuery.CacheMinutes)
        };

        _cache.Set(cacheKey, response, cacheOptions);

        return response;
    }
}
```

### How it works

```
Request comes in (e.g., GetLoanProductsQuery)
    │
    ▼
CachingBehaviour checks: Does request implement ICacheableQuery?
    │
    ├── No → Pass through to next handler
    │
    └── Yes → Check cache for key "loan-products-all"
        │
        ├── Cache HIT → Return cached response (skip handler entirely)
        │
        └── Cache MISS → Execute handler → Store result → Return
```

---

## DI Registration

```csharp
// src/LoanSuperMarket.Application/DependencyInjection.cs
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
```

The behaviour is registered as an open generic — it applies to ALL MediatR requests, but only activates for those implementing `ICacheableQuery`.

### Pipeline Order

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));      // 1st
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));   // 2nd
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));    // 3rd
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));       // 4th
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccountStatusBehaviour<,>)); // 5th
```

Caching runs AFTER validation — this ensures invalid requests are never cached.

---

## Cache-Aside Pattern Explained

```
┌─────────────┐     ┌─────────────┐     ┌──────────────┐
│   Client    │────▶│    Cache    │────▶│   Database   │
│  (Blazor)   │     │ (IMemoryCache)│    │ (SQL Server) │
└─────────────┘     └─────────────┘     └──────────────┘

Read path:
1. Check cache → if found, return (cache hit)
2. If not found → query database → store in cache → return

Write path:
1. Write to database
2. Invalidate related cache entries
```

---

## Cache Invalidation on Mutations

When data changes, cached entries must be invalidated. This is done in command handlers:

```csharp
// In CreateLoanProductCommandHandler
public async Task<Guid> Handle(
    CreateLoanProductCommand request, CancellationToken ct)
{
    // ... create the product ...

    // Invalidate the products list cache
    _cache.Remove("loan-products-all");

    return product.Id;
}
```

### Invalidation patterns

```csharp
// After creating/updating/deleting a loan product:
_cache.Remove("loan-products-all");
_cache.Remove($"loan-product-{productId}");

// After a loan application status change:
_cache.Remove("dashboard-summary");

// After a payment is recorded:
_cache.Remove("dashboard-summary");
_cache.Remove($"borrower-loans-{borrowerId}");
```

### Injecting IMemoryCache in handlers

```csharp
public sealed class CreateLoanProductCommandHandler
    : IRequestHandler<CreateLoanProductCommand, Guid>
{
    private readonly ILoanProductRepository _repository;
    private readonly IMemoryCache _cache;

    public CreateLoanProductCommandHandler(
        ILoanProductRepository repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Guid> Handle(...)
    {
        // ... business logic ...
        _cache.Remove("loan-products-all");
        return product.Id;
    }
}
```

---

## When to Cache vs When Not to Cache

### Good candidates for caching

| Query | Why | TTL |
|-------|-----|-----|
| Loan products list | Rarely changes, frequently accessed | 10 min |
| Dashboard summary | Expensive aggregation, acceptable staleness | 2 min |
| Loan product by ID | Rarely changes | 5 min |
| Platform statistics | Expensive, changes slowly | 15 min |

### Bad candidates for caching

| Query | Why |
|-------|-----|
| Borrower's applications | User-specific, changes frequently |
| Funding queue | Real-time accuracy needed |
| Payment history | Must be current |
| Audit logs | Must be current |
| Any user-scoped data | Cache key would need user ID, reducing hit rate |

### Rules of thumb

1. **Cache read-heavy, write-light data** — Products, categories, configuration.
2. **Don't cache user-specific data** — Unless you include the user ID in the cache key.
3. **Don't cache real-time data** — Funding queue, payment status.
4. **Short TTL for dashboards** — 1-2 minutes is acceptable staleness.
5. **Longer TTL for reference data** — Products, roles, permissions.

---

## Configuration

### Cache entry options

```csharp
var cacheOptions = new MemoryCacheEntryOptions
{
    // Absolute expiration: entry removed after X minutes regardless of access
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),

    // Sliding expiration: entry removed if not accessed for X minutes
    // SlidingExpiration = TimeSpan.FromMinutes(2),

    // Priority: which entries to evict first under memory pressure
    Priority = CacheItemPriority.Normal,

    // Size: for memory-bounded caches
    // Size = 1
};
```

### Memory cache size limits (optional)

```csharp
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Maximum number of cache entries
});
```

---

## Advanced: Cache Key Strategies

### Simple key (no parameters)

```csharp
public string CacheKey => "loan-products-all";
```

### Parameterized key

```csharp
public sealed record GetLoanProductsByStatusQuery(string Status)
    : IRequest<IReadOnlyList<LoanProductDto>>, ICacheableQuery
{
    public string CacheKey => $"loan-products-status-{Status.ToLowerInvariant()}";
}
```

### Paged query key

```csharp
public sealed record GetLoanProductsPagedQuery(int Page, int PageSize)
    : IRequest<PagedResult<LoanProductDto>>, ICacheableQuery
{
    public string CacheKey => $"loan-products-page-{Page}-size-{PageSize}";
    public int CacheMinutes => 3;
}
```

---

## Step-by-Step Extension Guide

### Making an existing query cacheable

1. Add `ICacheableQuery` to the query record:
   ```csharp
   public sealed record GetBorrowersQuery
       : IRequest<IReadOnlyList<BorrowerDto>>, ICacheableQuery
   {
       public string CacheKey => "borrowers-all";
       public int CacheMinutes => 5;
   }
   ```

2. Add invalidation in related command handlers:
   ```csharp
   // In CreateBorrowerCommandHandler
   _cache.Remove("borrowers-all");
   ```

That's it — the `CachingBehaviour` automatically handles the rest.

### Switching to distributed cache (Redis)

1. Replace `AddMemoryCache()` with `AddStackExchangeRedisCache()`
2. Change `IMemoryCache` to `IDistributedCache`
3. Update `CachingBehaviour` to use `GetStringAsync`/`SetStringAsync` with JSON serialization
4. This enables cache sharing across multiple API instances

---

## Testing Considerations

- **Unit test the behaviour:** Mock `IMemoryCache`, verify cache hit returns without calling `next()`.
- **Verify invalidation:** After a mutation, verify the cache key is removed.
- **Performance test:** Measure response time with and without cache to validate improvement.

---

## Common Pitfalls

1. **Stale data** — If you forget to invalidate after a mutation, users see outdated data. Always invalidate in command handlers.
2. **Cache key collisions** — Use descriptive, unique keys. Include all parameters that affect the result.
3. **Memory pressure** — In-memory cache grows unbounded unless you set `SizeLimit`. Monitor memory usage.
4. **Serialization issues** — `IMemoryCache` stores object references (no serialization). If you switch to distributed cache, ensure DTOs are serializable.
5. **Cache stampede** — Multiple concurrent requests for the same uncached key all hit the database. For high-traffic scenarios, consider a lock or `GetOrCreateAsync`.
