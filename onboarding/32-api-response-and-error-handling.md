# 32 — API Response & Error Handling

## Overview

The platform uses a consistent API response wrapper (`ApiResponse<T>`) and a global exception middleware to ensure all responses — success or failure — follow the same JSON structure. This makes client-side error handling predictable and simplifies debugging with correlation IDs for request tracing.

---

## Feature Requirements (Plain English)

1. All API responses use the same JSON structure: `{ success, message, data, errors }`.
2. Domain validation errors return 400 with specific error messages.
3. Application validation errors (FluentValidation) return 400 with a list of field errors.
4. Unhandled exceptions return 500 with a generic message (no stack traces in production).
5. Every request gets a correlation ID for tracing through logs.
6. The Blazor client can reliably check `response.Success` and display `response.Errors`.

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Response wrapper | ApiResponse<T> | Envelope pattern |
| Exception handling | Middleware | Pipeline pattern |
| Request tracing | CorrelationIdMiddleware | Cross-cutting concern |
| Validation | FluentValidation + MediatR pipeline | Fail-fast validation |

---

## ApiResponse<T> Wrapper

```csharp
// src/LoanSuperMarket.Shared/Common/ApiResponse.cs
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

### JSON Response Examples

**Success:**
```json
{
  "success": true,
  "message": "Loan application created successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Draft"
  },
  "errors": []
}
```

**Validation failure:**
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": [
    "Requested amount must be between £1,000 and £100,000.",
    "Term must be between 6 and 60 months."
  ]
}
```

**Domain error:**
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": [
    "Cannot approve an application that is not under review."
  ]
}
```

**Server error:**
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": [
    "An unexpected error occurred. Please try again later."
  ]
}
```

---

## GlobalExceptionMiddleware

```csharp
// src/LoanSuperMarket.Api/Middleware/GlobalExceptionMiddleware.cs
using System.Net;
using LoanSuperMarket.Application.Common.Models;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Shared.Common;

namespace LoanSuperMarket.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
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
            // FluentValidation failures → 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Errors.ToList());
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException exception)
        {
            // Domain rule violations → 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Message);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception exception)
        {
            // Everything else → 500 Internal Server Error
            _logger.LogError(exception, "Unhandled exception occurred.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(
                "An unexpected error occurred. Please try again later.");
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
```

### Exception Type Hierarchy

```
Exception (base)
├── DomainException → 400 Bad Request
│   └── Business rule violations (e.g., "Cannot fund an already funded loan")
├── ApplicationValidationException → 400 Bad Request
│   └── FluentValidation failures (e.g., "Amount is required", "Email is invalid")
├── UnauthorizedAccessException → 401 (handled by auth middleware)
└── Everything else → 500 Internal Server Error
```

---

## DomainException

```csharp
// src/LoanSuperMarket.Domain/Common/DomainException.cs
namespace LoanSuperMarket.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
```

Thrown from domain entities when business rules are violated:
```csharp
// In LoanApplication entity
public void Approve()
{
    if (Status != LoanApplicationStatus.UnderReview)
        throw new DomainException("Cannot approve an application that is not under review.");

    Status = LoanApplicationStatus.Approved;
}
```

---

## ApplicationValidationException

```csharp
// src/LoanSuperMarket.Application/Common/Models/ApplicationValidationException.cs
namespace LoanSuperMarket.Application.Common.Models;

public sealed class ApplicationValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ApplicationValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList();
    }
}
```

Thrown by the `ValidationBehaviour` MediatR pipeline:
```csharp
// ValidationBehaviour.cs
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    var validators = _validators.ToList();
    if (validators.Count == 0)
        return await next(cancellationToken);

    var context = new ValidationContext<TRequest>(request);
    var results = await Task.WhenAll(
        validators.Select(v => v.ValidateAsync(context, cancellationToken)));

    var errors = results
        .SelectMany(r => r.Errors)
        .Where(f => f is not null)
        .Select(f => f.ErrorMessage)
        .Distinct()
        .ToList();

    if (errors.Count > 0)
        throw new ApplicationValidationException(errors);

    return await next(cancellationToken);
}
```

---

## CorrelationIdMiddleware

```csharp
// src/LoanSuperMarket.Api/Middleware/CorrelationIdMiddleware.cs
namespace LoanSuperMarket.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use client-provided correlation ID or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Add to response headers for client-side tracing
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to logging scope so all log entries include it
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
```

### How it helps debugging

Every log entry within a request includes the correlation ID:
```
[2024-01-15 10:30:45] [CorrelationId: abc123] INFO: Processing GetDashboardSummaryQuery
[2024-01-15 10:30:45] [CorrelationId: abc123] ERROR: Database timeout
```

The client receives the correlation ID in the response header and can include it in bug reports.

---

## Middleware Registration Order

```csharp
// Program.cs — ORDER MATTERS
app.UseMiddleware<CorrelationIdMiddleware>();  // First: assigns correlation ID
app.UseMiddleware<GlobalExceptionMiddleware>(); // Second: catches all exceptions

// ... rest of pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

The correlation ID middleware runs first so that if the exception middleware catches an error, the correlation ID is already in the logging scope.

---

## How Errors Flow: Domain → API → Blazor UI

### Flow 1: Domain Validation Error

```
1. Blazor: FundingApiClient.FundLoanAsync(applicationId)
2. API: FundingController → MediatR → FundLoanCommandHandler
3. Domain: loanApplication.Fund() throws DomainException("Insufficient lender capital")
4. Middleware: GlobalExceptionMiddleware catches DomainException
5. API Response: 400 { success: false, errors: ["Insufficient lender capital"] }
6. Blazor: response.Success == false → ToastService.ShowError("Error", errors[0])
```

### Flow 2: FluentValidation Error

```
1. Blazor: WizardApiClient.CreateDraftAsync(amount: -1000, term: 0)
2. API: WizardController → MediatR → ValidationBehaviour
3. Validation: CreateDraftValidator fails:
   - "Amount must be positive"
   - "Term must be at least 6 months"
4. Pipeline: throws ApplicationValidationException(errors)
5. Middleware: catches ApplicationValidationException → 400
6. API Response: { success: false, errors: ["Amount must be positive", "Term must be at least 6 months"] }
7. Blazor: displays errors in form validation summary
```

### Flow 3: Unhandled Exception

```
1. Blazor: DashboardApiClient.GetSummaryAsync()
2. API: DashboardController → Handler → Repository
3. Infrastructure: SQL Server is down → SqlException
4. Middleware: catches Exception, logs full stack trace
5. API Response: 500 { success: false, errors: ["An unexpected error occurred..."] }
6. Blazor: shows generic error message (no stack trace exposed)
```

---

## HTTP Status Code Mapping

| Exception Type | HTTP Status | When |
|---------------|-------------|------|
| ApplicationValidationException | 400 Bad Request | FluentValidation failures |
| DomainException | 400 Bad Request | Business rule violations |
| UnauthorizedAccessException | 401 Unauthorized | Missing/invalid token |
| ForbiddenAccessException | 403 Forbidden | Insufficient permissions |
| NotFoundException | 404 Not Found | Entity not found |
| Exception (generic) | 500 Internal Server Error | Unexpected errors |

---

## Blazor Client-Side Error Handling Pattern

```csharp
// Standard pattern in API clients
public async Task<ApiResponse<T>?> GetAsync<T>(string url)
{
    try
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<T>>(url);
    }
    catch (HttpRequestException)
    {
        return ApiResponse<T>.Fail("Network error. Please check your connection.");
    }
}

// Standard pattern in components
var response = await ApiClient.GetSummaryAsync();
if (response?.Success == true)
{
    _data = response.Data;
}
else
{
    _error = response?.Errors.FirstOrDefault() ?? "An error occurred.";
    ToastService.ShowError("Error", _error);
}
```

---

## Step-by-Step Extension Guide

### Adding a new exception type (e.g., NotFoundException)

1. **Create the exception:**
   ```csharp
   public class NotFoundException : Exception
   {
       public NotFoundException(string entityName, object key)
           : base($"{entityName} with key '{key}' was not found.") { }
   }
   ```

2. **Add to middleware:**
   ```csharp
   catch (NotFoundException exception)
   {
       context.Response.StatusCode = (int)HttpStatusCode.NotFound;
       context.Response.ContentType = "application/json";
       var response = ApiResponse<object>.Fail(exception.Message);
       await context.Response.WriteAsJsonAsync(response);
   }
   ```

3. **Throw from handlers:**
   ```csharp
   var entity = await _repo.GetByIdAsync(id, ct)
       ?? throw new NotFoundException("LoanApplication", id);
   ```

### Adding problem details (RFC 7807)

For standards-compliant error responses, you could extend the middleware to return ProblemDetails:
```csharp
var problemDetails = new ProblemDetails
{
    Status = 400,
    Title = "Validation Error",
    Detail = string.Join("; ", errors),
    Instance = context.Request.Path,
    Extensions = { ["correlationId"] = correlationId }
};
```

---

## Common Pitfalls

1. **Exposing stack traces** — Never include `exception.StackTrace` in production responses. Log it server-side only.
2. **Catching too broadly** — Don't catch `Exception` in controllers. Let the middleware handle it.
3. **Inconsistent responses** — Always use `ApiResponse<T>`. Never return raw strings or anonymous objects.
4. **Missing content type** — Always set `context.Response.ContentType = "application/json"` before writing.
5. **Double-writing response** — Check `context.Response.HasStarted` before writing in middleware.
