# 04 — FluentValidation in LoanSuperMarket

## Table of Contents

1. [What Is FluentValidation?](#what-is-fluentvalidation)
2. [Why FluentValidation Over DataAnnotations?](#why-fluentvalidation-over-dataannotations)
3. [The AbstractValidator Pattern](#the-abstractvalidator-pattern)
4. [Common Validation Rules](#common-validation-rules)
5. [How ValidationBehaviour Integrates with MediatR](#how-validationbehaviour-integrates-with-mediatr)
6. [How Validators Are Auto-Discovered](#how-validators-are-auto-discovered)
7. [Error Response Format](#error-response-format)
8. [Full Example: CreateLoanProductCommandValidator](#full-example-createloanproductcommandvalidator)
9. [Second Example: UploadDocumentCommandValidator](#second-example-uploaddocumentcommandvalidator)
10. [How to Add a New Validator Step-by-Step](#how-to-add-a-new-validator-step-by-step)
11. [Custom Validation Rules](#custom-validation-rules)
12. [Testing Validators](#testing-validators)
13. [Common Pitfalls](#common-pitfalls)

---

## What Is FluentValidation?

FluentValidation is a .NET library for building strongly-typed validation rules using a fluent
interface. Instead of scattering `[Required]` and `[Range]` attributes across your models, you
write dedicated validator classes that express business rules as readable C# code.

In LoanSuperMarket, every MediatR command that modifies data passes through a validation pipeline
**before** it reaches the handler. If validation fails, the request is rejected with a structured
error response — the handler never executes.

**NuGet Package:** `FluentValidation.DependencyInjectionExtensions`

---

## Why FluentValidation Over DataAnnotations?

| Concern | DataAnnotations | FluentValidation |
|---------|----------------|-----------------|
| **Separation of concerns** | Rules live on the model itself | Rules live in dedicated classes |
| **Complex rules** | Limited (custom attributes are verbose) | First-class support for conditional, cross-property, async rules |
| **Testability** | Hard to unit test attributes | Validators are plain classes — easy to test |
| **Reusability** | Attributes are per-property | Rules can be composed, shared, and inherited |
| **Readability** | Clutters model with attributes | Reads like English: `RuleFor(x => x.Title).NotEmpty()` |
| **Dependency injection** | Not supported | Validators can inject services (e.g., check DB uniqueness) |
| **MediatR integration** | Manual wiring | Automatic via pipeline behaviours |

**Our decision:** DataAnnotations are fine for simple DTOs on the Blazor client for immediate
UI feedback. But all **server-side business validation** uses FluentValidation in the Application
layer, ensuring the domain is never reached with invalid data.

---

## The AbstractValidator Pattern

Every validator in our codebase inherits from `AbstractValidator<T>` where `T` is the MediatR
request (command or query) being validated.

```csharp
using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed class CreateLoanProductCommandValidator : AbstractValidator<CreateLoanProductCommand>
{
    public CreateLoanProductCommandValidator()
    {
        // All rules are defined in the constructor
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(150);
    }
}
```

### Key Conventions in This Project

1. **One validator per command** — placed in the same folder as the command
2. **Naming:** `{CommandName}Validator` (e.g., `CreateLoanProductCommandValidator`)
3. **Sealed classes** — validators are never inherited
4. **Constructor-only rules** — all rules defined in the constructor (no method-based setup)
5. **No async rules in constructor** — if you need async (e.g., DB checks), use `MustAsync`

### File Location Pattern

```
src/LoanSuperMarket.Application/
└── Features/
    └── LoanProducts/
        └── CreateLoanProduct/
            ├── CreateLoanProductCommand.cs           ← The MediatR request
            ├── CreateLoanProductCommandHandler.cs    ← The handler
            └── CreateLoanProductCommandValidator.cs  ← The validator (THIS FILE)
```

---

## Common Validation Rules

Here's a reference of every rule type used in this project, with explanations:

### NotEmpty

Ensures a string is not null, not empty, and not whitespace. For GUIDs, ensures it's not
`Guid.Empty`. For collections, ensures at least one item.

```csharp
RuleFor(x => x.Title)
    .NotEmpty();
// Fails for: null, "", "   "

RuleFor(x => x.LenderId)
    .NotEmpty();
// Fails for: Guid.Empty (00000000-0000-0000-0000-000000000000)
```

### GreaterThan

Ensures a numeric value exceeds a threshold. Can compare against a constant or another property.

```csharp
RuleFor(x => x.MinimumAmount)
    .GreaterThan(0);
// Fails for: 0, -1, -100

RuleFor(x => x.MaximumAmount)
    .GreaterThanOrEqualTo(x => x.MinimumAmount);
// Cross-property: MaximumAmount must be >= MinimumAmount
```

### MaximumLength

Limits string length. Use this for properties that map to database columns with length constraints.

```csharp
RuleFor(x => x.Title)
    .MaximumLength(150);
// Fails for: any string longer than 150 characters
// Matches the database column: HasMaxLength(150)
```

### LessThanOrEqualTo

Sets an upper bound on numeric values.

```csharp
RuleFor(x => x.InterestRate)
    .LessThanOrEqualTo(100);
// Interest rate can't exceed 100%
```

### InclusiveBetween

Validates that a value falls within a range (inclusive on both ends).

```csharp
RuleFor(x => x.TermMonths)
    .InclusiveBetween(1, 600);
// Term must be between 1 and 600 months (50 years max)
```

### IsInEnum

Validates that an enum value is a defined member of the enum type.

```csharp
RuleFor(x => x.DocumentType)
    .IsInEnum();
// Fails for: (DocumentType)999 — any value not defined in the enum
```

### NotNull

Ensures a reference type is not null (but allows empty strings — use NotEmpty for strings).

```csharp
RuleFor(x => x.FileStream)
    .NotNull()
    .WithMessage("File is required.");
// Fails for: null
```

### WithMessage

Overrides the default error message for any rule.

```csharp
RuleFor(x => x.FileStream)
    .NotNull()
    .WithMessage("File is required.");
// Default would be: "'File Stream' must not be empty."
// Custom message: "File is required."
```

### Must (Custom Predicate)

Executes a custom boolean expression. Use for business rules that don't fit built-in validators.

```csharp
RuleFor(x => x.Email)
    .Must(email => email.EndsWith("@company.com"))
    .WithMessage("Only company email addresses are allowed.");
```

### When (Conditional Rules)

Applies rules only when a condition is met.

```csharp
RuleFor(x => x.CoSignerName)
    .NotEmpty()
    .When(x => x.RequestedAmount > 50_000m)
    .WithMessage("A co-signer is required for loans over £50,000.");
```

---

## How ValidationBehaviour Integrates with MediatR

The magic happens in `ValidationBehaviour<TRequest, TResponse>` — a MediatR pipeline behaviour
that intercepts every request, runs all registered validators, and throws if any fail.

### The Full Implementation

```csharp
// File: src/LoanSuperMarket.Application/Common/Behaviours/ValidationBehaviour.cs

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
        // 1. If no validators are registered for this request type, skip validation
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        // 2. Create a validation context wrapping the request
        var context = new ValidationContext<TRequest>(request);

        // 3. Run ALL validators in parallel
        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        // 4. Collect all error messages, deduplicate
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToList();

        // 5. If any errors exist, throw — the handler NEVER executes
        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }

        // 6. Validation passed — continue to the handler
        return await next(cancellationToken);
    }
}
```

### How It Works Step by Step

```
┌─────────────────────────────────────────────────────────────────┐
│  Client sends POST /api/loan-products                           │
│  Body: { "title": "", "minimumAmount": -5, ... }                │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  API Controller deserializes → CreateLoanProductCommand          │
│  Sends to MediatR: _mediator.Send(command)                      │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│  MediatR Pipeline Behaviours (in order):                        │
│  1. LoggingBehaviour        ← logs the request                  │
│  2. PerformanceBehaviour    ← starts timer                      │
│  3. ValidationBehaviour     ← ⚡ VALIDATES HERE                 │
│  4. CachingBehaviour        ← cache check                       │
│  5. AccountStatusBehaviour  ← user account active?              │
│  6. LimitEnforcementBehaviour ← within limits?                  │
│  7. ResourceAuthorizationBehaviour ← authorized?                │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼ (if validation fails)
┌─────────────────────────────────────────────────────────────────┐
│  ApplicationValidationException thrown                           │
│  Caught by GlobalExceptionMiddleware                            │
│  Returns HTTP 400 with error list                               │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **All validators run in parallel** — `Task.WhenAll` means the user sees ALL errors at once,
   not one at a time
2. **Errors are deduplicated** — `.Distinct()` prevents duplicate messages
3. **No validators = skip** — queries without validators pass through without overhead
4. **Exception-based flow** — throwing `ApplicationValidationException` ensures the handler
   never executes with invalid data

---

## How Validators Are Auto-Discovered

You never manually register validators. The `DependencyInjection.cs` file in the Application
layer handles everything:

```csharp
// File: src/LoanSuperMarket.Application/DependencyInjection.cs

using FluentValidation;
using LoanSuperMarket.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LoanSuperMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register all MediatR handlers from this assembly
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        // ⚡ AUTO-DISCOVER all validators in this assembly
        // Scans for any class that implements AbstractValidator<T>
        // Registers them as IValidator<T> in the DI container
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviours (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccountStatusBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LimitEnforcementBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceAuthorizationBehaviour<,>));

        return services;
    }
}
```

### What `AddValidatorsFromAssembly` Does

1. Scans the assembly for all classes inheriting `AbstractValidator<T>`
2. Registers each as `IValidator<T>` with **Transient** lifetime
3. When `ValidationBehaviour` is constructed, DI injects `IEnumerable<IValidator<TRequest>>`
4. If no validator exists for a request type, the enumerable is empty → validation is skipped

### What This Means for You

- **Just create the validator class** — it's automatically picked up
- **No registration code needed** — no `services.AddScoped<IValidator<...>, ...>()`
- **Multiple validators per command** — if you create two validators for the same command,
  both will run (useful for separating concerns)

---

## Error Response Format

When validation fails, the error flows through three components:

### 1. ApplicationValidationException (thrown by ValidationBehaviour)

```csharp
// File: src/LoanSuperMarket.Application/Common/Models/ApplicationValidationException.cs

namespace LoanSuperMarket.Application.Common.Models;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
```

### 2. GlobalExceptionMiddleware (catches and formats the response)

```csharp
// File: src/LoanSuperMarket.Api/Middleware/GlobalExceptionMiddleware.cs

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
            // ⚡ Validation errors → HTTP 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Errors.ToList());

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException exception)
        {
            // Domain rule violations → HTTP 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Message);

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception exception)
        {
            // Unexpected errors → HTTP 500
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

### 3. ApiResponse<T> (the response envelope)

```csharp
// File: src/LoanSuperMarket.Shared/Common/ApiResponse.cs

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

### Example HTTP Response (Validation Failure)

```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "success": false,
  "message": null,
  "data": null,
  "errors": [
    "'Title' must not be empty.",
    "'Minimum Amount' must be greater than '0'.",
    "'Maximum Amount' must be greater than or equal to 'Minimum Amount'.",
    "'Lender Id' must not be empty."
  ]
}
```

### The Complete Error Flow

```
Validator fails
    → ValidationBehaviour throws ApplicationValidationException(errors)
        → GlobalExceptionMiddleware catches it
            → Returns ApiResponse<object>.Fail(errors) with HTTP 400
                → Blazor client reads response.Errors list
                    → Displays errors in UI
```

---

## Full Example: CreateLoanProductCommandValidator

This is the most comprehensive validator in the project. Let's examine every rule.

### The Command Being Validated

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommand.cs

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

### The Validator (Every Rule Explained)

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanProducts/CreateLoanProduct/CreateLoanProductCommandValidator.cs

using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed class CreateLoanProductCommandValidator : AbstractValidator<CreateLoanProductCommand>
{
    public CreateLoanProductCommandValidator()
    {
        // ─── RULE 1: Title must not be empty and max 150 chars ───────────
        // Why 150? Matches the database column: HasMaxLength(150) in LoanProductConfiguration
        // NotEmpty covers: null, "", and whitespace-only strings
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(150);

        // ─── RULE 2: Description must not be empty and max 2000 chars ────
        // Longer limit for rich product descriptions
        // Matches: HasMaxLength(2000) in LoanProductConfiguration
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        // ─── RULE 3: MinimumAmount must be positive ──────────────────────
        // A loan product can't have a zero or negative minimum
        // This is a business rule: you can't offer a £0 loan
        RuleFor(x => x.MinimumAmount)
            .GreaterThan(0);

        // ─── RULE 4: MaximumAmount must be positive AND >= MinimumAmount ─
        // Two rules chained: first check it's positive, then check it's
        // at least as large as the minimum (cross-property validation)
        RuleFor(x => x.MaximumAmount)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumAmount);

        // ─── RULE 5: InterestRate between 0 and 100 ─────────────────────
        // Stored as a percentage (e.g., 10.5 means 10.5%)
        // Must be positive and can't exceed 100%
        RuleFor(x => x.InterestRate)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        // ─── RULE 6: MinimumTermMonths must be positive ──────────────────
        // At least 1 month term
        RuleFor(x => x.MinimumTermMonths)
            .GreaterThan(0);

        // ─── RULE 7: MaximumTermMonths positive AND >= MinimumTermMonths ─
        // Same pattern as amount: max must be >= min
        RuleFor(x => x.MaximumTermMonths)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumTermMonths);

        // ─── RULE 8: LenderId must not be empty GUID ─────────────────────
        // Every product must belong to a lender
        // NotEmpty on Guid checks for Guid.Empty
        RuleFor(x => x.LenderId)
            .NotEmpty();
    }
}
```

### What Happens When This Validator Runs

Given this invalid request:
```json
{
  "title": "",
  "description": "A valid description",
  "minimumAmount": 50000,
  "maximumAmount": 10000,
  "interestRate": 150,
  "minimumTermMonths": 24,
  "maximumTermMonths": 12,
  "lenderId": "00000000-0000-0000-0000-000000000000"
}
```

The response would be:
```json
{
  "success": false,
  "data": null,
  "errors": [
    "'Title' must not be empty.",
    "'Maximum Amount' must be greater than or equal to 'Minimum Amount'.",
    "'Interest Rate' must be less than or equal to '100'.",
    "'Maximum Term Months' must be greater than or equal to 'Minimum Term Months'.",
    "'Lender Id' must not be empty."
  ]
}
```

---

## Second Example: UploadDocumentCommandValidator

This validator demonstrates enum validation and custom messages:

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanApplications/UploadDocument/UploadDocumentCommandValidator.cs

using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;

public sealed class UploadDocumentCommandValidator
    : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        // Ensure the application ID is a valid, non-empty GUID
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        // Ensure the document type is a valid enum member
        // Prevents casting invalid integers to the enum
        RuleFor(x => x.DocumentType)
            .IsInEnum();

        // File name is required and limited to 500 chars
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        // The file stream itself must not be null
        // Custom message overrides the default "must not be empty" text
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File is required.");
    }
}
```

### Third Example: CreateDraftLoanApplicationCommandValidator

A simpler validator showing `InclusiveBetween`:

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanApplications/CreateDraftLoanApplication/CreateDraftLoanApplicationCommandValidator.cs

using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateDraftLoanApplication;

public sealed class CreateDraftLoanApplicationCommandValidator
    : AbstractValidator<CreateDraftLoanApplicationCommand>
{
    public CreateDraftLoanApplicationCommandValidator()
    {
        // Loan amount must be positive
        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0);

        // Term between 1 month and 600 months (50 years — covers mortgages)
        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 600);

        // Purpose is required and limited to 1000 chars
        // Matches database: HasMaxLength(1000) in LoanApplicationConfiguration
        RuleFor(x => x.Purpose)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
```

---

## How to Add a New Validator Step-by-Step

Let's say you're adding a new command `UpdateLoanProductCommand` and need validation.

### Step 1: Create the Command

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanProducts/UpdateLoanProduct/UpdateLoanProductCommand.cs

using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.UpdateLoanProduct;

public sealed record UpdateLoanProductCommand(
    Guid ProductId,
    string Title,
    string Description,
    decimal MinimumAmount,
    decimal MaximumAmount,
    decimal InterestRate,
    int MinimumTermMonths,
    int MaximumTermMonths) : IRequest;
```

### Step 2: Create the Validator in the Same Folder

```csharp
// File: src/LoanSuperMarket.Application/Features/LoanProducts/UpdateLoanProduct/UpdateLoanProductCommandValidator.cs

using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanProducts.UpdateLoanProduct;

public sealed class UpdateLoanProductCommandValidator
    : AbstractValidator<UpdateLoanProductCommand>
{
    public UpdateLoanProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

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
    }
}
```

### Step 3: That's It!

No registration needed. The validator is automatically discovered by
`services.AddValidatorsFromAssembly(assembly)` because it:
- Inherits from `AbstractValidator<T>`
- Lives in the same assembly (`LoanSuperMarket.Application`)

### Step 4: Verify It Works

Send an invalid request to the endpoint. You should get a 400 response with the errors list.

### Checklist for New Validators

- [ ] Class is `sealed`
- [ ] Class name follows pattern: `{CommandName}Validator`
- [ ] Inherits `AbstractValidator<TCommand>`
- [ ] Lives in the same folder as the command
- [ ] All string properties have `MaximumLength` matching the DB column
- [ ] All required properties have `NotEmpty()` or `NotNull()`
- [ ] Numeric ranges match business rules
- [ ] Cross-property rules use lambda references (e.g., `GreaterThanOrEqualTo(x => x.Min)`)

---

## Custom Validation Rules

For complex business logic that goes beyond built-in rules, FluentValidation offers several
approaches.

### Using Must (Synchronous Custom Rule)

```csharp
public sealed class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .Must(amount => amount % 0.01m == 0)
            .WithMessage("Amount must not have more than 2 decimal places.");

        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .Must((command, sourceId) => sourceId != command.DestinationAccountId)
            .WithMessage("Source and destination accounts must be different.");
    }
}
```

### Using MustAsync (Async Custom Rule with DI)

When you need to check the database (e.g., uniqueness), inject a service:

```csharp
public sealed class CreateBorrowerCommandValidator : AbstractValidator<CreateBorrowerCommand>
{
    private readonly ApplicationDbContext _context;

    // Validators support constructor injection!
    public CreateBorrowerCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail)
            .WithMessage("A borrower with this email already exists.");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _context.Borrowers
            .AnyAsync(b => b.Email == email, cancellationToken);
    }
}
```

### Using Custom() for Complex Multi-Field Validation

```csharp
public sealed class DateRangeCommandValidator : AbstractValidator<DateRangeCommand>
{
    public DateRangeCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty();

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");

        // Custom rule that adds multiple errors
        RuleFor(x => x)
            .Custom((command, context) =>
            {
                var duration = command.EndDate - command.StartDate;
                if (duration.TotalDays > 365)
                {
                    context.AddFailure("DateRange", "Date range cannot exceed 1 year.");
                }
                if (command.StartDate < DateTime.UtcNow.Date)
                {
                    context.AddFailure("StartDate", "Start date cannot be in the past.");
                }
            });
    }
}
```

### Using When/Unless for Conditional Rules

```csharp
public sealed class LoanApplicationCommandValidator : AbstractValidator<LoanApplicationCommand>
{
    public LoanApplicationCommandValidator()
    {
        // Only validate co-signer details when amount exceeds threshold
        RuleFor(x => x.CoSignerName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.RequestedAmount > 100_000m)
            .WithMessage("A co-signer is required for loans over £100,000.");

        // Unless the borrower has Premium tier, require employment proof
        RuleFor(x => x.EmploymentProofDocumentId)
            .NotEmpty()
            .Unless(x => x.BorrowerTier == CreditTier.Premium)
            .WithMessage("Employment proof is required for non-Premium borrowers.");
    }
}
```

### Using RuleSet for Grouped Validation

```csharp
public sealed class LoanProductCommandValidator : AbstractValidator<LoanProductCommand>
{
    public LoanProductCommandValidator()
    {
        // These rules always run
        RuleFor(x => x.Title).NotEmpty();

        // These rules only run when explicitly requested
        RuleSet("Publishing", () =>
        {
            RuleFor(x => x.Description)
                .MinimumLength(50)
                .WithMessage("Published products need a detailed description (50+ chars).");

            RuleFor(x => x.InterestRate)
                .LessThanOrEqualTo(30)
                .WithMessage("Published products cannot exceed 30% APR.");
        });
    }
}
```

### Using Include to Compose Validators

```csharp
// Base address validator
public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostCode).NotEmpty().MaximumLength(10);
    }
}

// Command validator that includes address validation
public sealed class UpdateBorrowerCommandValidator : AbstractValidator<UpdateBorrowerCommand>
{
    public UpdateBorrowerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();

        // Reuse the address validator for the nested Address property
        RuleFor(x => x.Address).SetValidator(new AddressValidator());
    }
}
```

---

## Testing Validators

Validators are plain classes — they're trivial to unit test.

### Basic Validator Test

```csharp
using FluentValidation.TestHelper;
using LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

namespace LoanSuperMarket.Application.Tests.Validators;

public class CreateLoanProductCommandValidatorTests
{
    private readonly CreateLoanProductCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var command = new CreateLoanProductCommand(
            Title: "",
            Description: "Valid description",
            MinimumAmount: 1000m,
            MaximumAmount: 50000m,
            InterestRate: 10m,
            MinimumTermMonths: 12,
            MaximumTermMonths: 60,
            LenderId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Have_Error_When_MaxAmount_Less_Than_MinAmount()
    {
        var command = new CreateLoanProductCommand(
            Title: "Valid Title",
            Description: "Valid description",
            MinimumAmount: 50000m,
            MaximumAmount: 10000m,  // Less than minimum!
            InterestRate: 10m,
            MinimumTermMonths: 12,
            MaximumTermMonths: 60,
            LenderId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MaximumAmount);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        var command = new CreateLoanProductCommand(
            Title: "Personal Loan",
            Description: "A flexible personal loan product",
            MinimumAmount: 1000m,
            MaximumAmount: 50000m,
            InterestRate: 10.5m,
            MinimumTermMonths: 12,
            MaximumTermMonths: 60,
            LenderId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_InterestRate_Not_Positive(decimal rate)
    {
        var command = new CreateLoanProductCommand(
            Title: "Valid",
            Description: "Valid",
            MinimumAmount: 1000m,
            MaximumAmount: 50000m,
            InterestRate: rate,
            MinimumTermMonths: 12,
            MaximumTermMonths: 60,
            LenderId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.InterestRate);
    }
}
```

### Testing Async Validators

```csharp
public class CreateBorrowerCommandValidatorTests
{
    [Fact]
    public async Task Should_Have_Error_When_Email_Already_Exists()
    {
        // Arrange — use in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Borrowers.Add(Borrower.Create("John", "Doe", "existing@test.com", "123", DateTime.Now));
        await context.SaveChangesAsync();

        var validator = new CreateBorrowerCommandValidator(context);

        var command = new CreateBorrowerCommand(
            Email: "existing@test.com",  // Already exists!
            FirstName: "Jane",
            LastName: "Doe");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("A borrower with this email already exists.");
    }
}
```

---

## Common Pitfalls

### 1. Forgetting MaximumLength

If your entity configuration has `HasMaxLength(150)` but your validator doesn't enforce it,
the database will throw a `DbUpdateException` at runtime. Always mirror DB constraints in
your validator.

```csharp
// ❌ BAD — will crash at database level
RuleFor(x => x.Title).NotEmpty();

// ✅ GOOD — catches it before hitting the DB
RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
```

### 2. Using NotNull Instead of NotEmpty for Strings

```csharp
// ❌ BAD — allows empty strings "" and whitespace "   "
RuleFor(x => x.Title).NotNull();

// ✅ GOOD — rejects null, empty, and whitespace
RuleFor(x => x.Title).NotEmpty();
```

### 3. Not Using Cross-Property Validation

```csharp
// ❌ BAD — doesn't check relationship between min and max
RuleFor(x => x.MinimumAmount).GreaterThan(0);
RuleFor(x => x.MaximumAmount).GreaterThan(0);

// ✅ GOOD — ensures max >= min
RuleFor(x => x.MinimumAmount).GreaterThan(0);
RuleFor(x => x.MaximumAmount)
    .GreaterThan(0)
    .GreaterThanOrEqualTo(x => x.MinimumAmount);
```

### 4. Putting Validation Logic in the Handler

```csharp
// ❌ BAD — validation in the handler
public async Task<Guid> Handle(CreateLoanProductCommand request, CancellationToken ct)
{
    if (string.IsNullOrEmpty(request.Title))
        throw new ArgumentException("Title is required");
    // ...
}

// ✅ GOOD — validation in the validator, handler assumes valid data
public async Task<Guid> Handle(CreateLoanProductCommand request, CancellationToken ct)
{
    // request is guaranteed valid by this point
    var product = LoanProduct.Create(request.Title, ...);
    // ...
}
```

### 5. Validators for Queries

You generally don't need validators for queries unless they have parameters that could cause
issues (e.g., negative page numbers, SQL injection in search terms).

```csharp
// Only validate queries when they have meaningful constraints
public sealed class GetLoanProductsPagedQueryValidator
    : AbstractValidator<GetLoanProductsPagedQuery>
{
    public GetLoanProductsPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```

---

## Summary

| Concept | Location |
|---------|----------|
| Validator base class | `AbstractValidator<T>` from FluentValidation |
| Pipeline integration | `ValidationBehaviour.cs` in `Common/Behaviours/` |
| Auto-discovery | `services.AddValidatorsFromAssembly(assembly)` in `DependencyInjection.cs` |
| Exception type | `ApplicationValidationException` in `Common/Models/` |
| Error response | `ApiResponse<T>.Fail(errors)` via `GlobalExceptionMiddleware` |
| HTTP status | 400 Bad Request |
| Validator location | Same folder as the command it validates |
| Naming convention | `{CommandName}Validator` |

---

*Next: [05 — Database and EF Core](./05-database-and-ef-core.md)*
