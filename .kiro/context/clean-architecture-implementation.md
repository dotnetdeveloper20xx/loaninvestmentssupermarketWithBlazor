# Clean Architecture Implementation Guide

## 🎯 Clean Architecture in Practice

This project demonstrates **production-ready Clean Architecture** implementation with:

- **Proper dependency flow** (inward dependencies only)
- **Rich domain models** with business logic encapsulation
- **CQRS pattern** for scalable read/write operations
- **Repository abstraction** for testable data access
- **Domain-driven design** principles throughout

## 🏗️ Layer Responsibilities & Dependencies

### Dependency Flow
```
┌─────────────────────────────────────────┐
│              Blazor UI                  │ ──┐
├─────────────────────────────────────────┤   │
│               API Layer                 │ ──┤
├─────────────────────────────────────────┤   │ Dependencies
│           Application Layer             │ ──┤ flow inward
├─────────────────────────────────────────┤   │
│             Domain Layer                │ ←─┘
├─────────────────────────────────────────┤
│          Infrastructure Layer           │
└─────────────────────────────────────────┘
```

### Layer Breakdown

#### 1. Domain Layer (Core Business Logic)
**Location**: `src/LoanSuperMarket.Domain/`

**Responsibilities**:
- Business entities with rich behavior
- Value objects for type safety
- Domain services for complex business rules
- Domain events for cross-aggregate communication
- Business rule validation

**Key Patterns**:
```csharp
// Rich domain entity with behavior
public sealed class LoanApplication : AuditableEntity
{
    // Private constructor prevents invalid state
    private LoanApplication() { }

    // Factory method with validation
    public static LoanApplication Create(
        Guid borrowerId,
        Guid loanProductId,
        Money requestedAmount,
        int termMonths,
        string purpose)
    {
        // Domain validation
        if (borrowerId == Guid.Empty)
            throw new DomainException("Borrower id is required.");
        
        // Business rule enforcement
        return new LoanApplication(borrowerId, loanProductId, requestedAmount, termMonths, purpose);
    }

    // Domain methods encapsulate business logic
    public void MarkUnderReview()
    {
        if (Status != LoanApplicationStatus.Submitted)
            throw new DomainException("Only submitted applications can move under review.");

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }
}

// Value objects for type safety
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "GBP")
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");
        
        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }
}
```

#### 2. Application Layer (Use Cases & Orchestration)
**Location**: `src/LoanSuperMarket.Application/`

**Responsibilities**:
- CQRS commands and queries
- Application services and handlers
- Cross-cutting concerns (validation, logging)
- Repository interfaces (abstractions)
- DTO definitions for data transfer

**Key Patterns**:
```csharp
// Command with validation
public sealed class CreateLoanApplicationCommand : IRequest<ApiResponse<Guid>>
{
    public Guid BorrowerId { get; set; }
    public Guid LoanProductId { get; set; }
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public string Purpose { get; set; } = string.Empty;
}

// Command handler with business orchestration
public sealed class CreateLoanApplicationCommandHandler 
    : IRequestHandler<CreateLoanApplicationCommand, ApiResponse<Guid>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _loanProductRepository;

    public async Task<ApiResponse<Guid>> Handle(
        CreateLoanApplicationCommand request, 
        CancellationToken cancellationToken)
    {
        // Validation and business rule checking
        var borrower = await _borrowerRepository.GetByIdAsync(request.BorrowerId);
        if (borrower is null)
            return ApiResponse<Guid>.Failure("Borrower not found.");

        // Domain object creation
        var money = Money.Create(request.RequestedAmount);
        var application = LoanApplication.Create(
            request.BorrowerId,
            request.LoanProductId,
            money,
            request.TermMonths,
            request.Purpose);

        // Persistence
        await _repository.AddAsync(application);
        await _repository.SaveChangesAsync();

        return ApiResponse<Guid>.Success(application.Id);
    }
}

// Query with server-side operations
public sealed class GetLoanApplicationsPagedQuery : IRequest<ApiResponse<PagedResult<LoanApplicationDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; }
}
```

#### 3. Infrastructure Layer (External Concerns)
**Location**: `src/LoanSuperMarket.Infrastructure/`

**Responsibilities**:
- Entity Framework Core configuration
- Repository implementations
- Database migrations
- External service integrations
- Caching implementations

**Key Patterns**:
```csharp
// Repository implementation
public sealed class LoanApplicationRepository : ILoanApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<LoanApplication?> GetByIdAsync(Guid id)
    {
        return await _context.LoanApplications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PagedResult<LoanApplicationDto>> GetPagedAsync(GridQueryRequest request)
    {
        var query = _context.LoanApplications.AsQueryable();

        // Server-side filtering
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Purpose.Contains(request.SearchTerm));
        }

        // Server-side sorting
        query = ApplySorting(query, request.SortColumn, request.SortDirection);

        var totalCount = await query.CountAsync();
        
        // DTO projection for performance
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LoanApplicationDto
            {
                Id = x.Id,
                Purpose = x.Purpose,
                Amount = x.RequestedAmount.Amount,
                Currency = x.RequestedAmount.Currency,
                Status = x.Status.ToString(),
                SubmittedAt = x.SubmittedAtUtc
            })
            .ToListAsync();

        return new PagedResult<LoanApplicationDto>(items, totalCount, request.Page, request.PageSize);
    }
}

// EF Core configuration
public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.HasKey(x => x.Id);

        // Value object mapping
        builder.OwnsOne(x => x.RequestedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("RequestedAmount")
                .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        // Enum mapping
        builder.Property(x => x.Status)
            .HasConversion<string>();
    }
}
```

#### 4. API Layer (External Interface)
**Location**: `src/LoanSuperMarket.Api/`

**Responsibilities**:
- REST API controllers
- Request/response handling
- Authentication and authorization
- Global exception handling
- API documentation (Swagger)

**Key Patterns**:
```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class LoanApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        CreateLoanApplicationCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(
            nameof(GetById), 
            new { id = result.Data }, 
            result);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanApplicationDto>>>> GetPaged(
        [FromQuery] GetLoanApplicationsPagedQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

// Global exception handling
public sealed class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericExceptionAsync(context, ex);
        }
    }
}
```

#### 5. Blazor UI Layer (Presentation)
**Location**: `src/LoanSuperMarket.Blazor/`

**Responsibilities**:
- User interface components
- Client-side state management
- API integration via typed clients
- User experience and workflows

## 🔄 CQRS Implementation

### Command Pattern
```csharp
// Commands modify state
public sealed class ApproveLoanApplicationCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
}

public sealed class ApproveLoanApplicationCommandHandler 
    : IRequestHandler<ApproveLoanApplicationCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(
        ApproveLoanApplicationCommand request, 
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id);
        if (application is null)
            return ApiResponse.Failure("Application not found.");

        // Domain method encapsulates business logic
        application.Approve();

        await _repository.SaveChangesAsync();
        return ApiResponse.Success();
    }
}
```

### Query Pattern
```csharp
// Queries return data without side effects
public sealed class GetLoanApplicationByIdQuery : IRequest<ApiResponse<LoanApplicationDetailDto>>
{
    public Guid Id { get; set; }
}

public sealed class GetLoanApplicationByIdQueryHandler 
    : IRequestHandler<GetLoanApplicationByIdQuery, ApiResponse<LoanApplicationDetailDto>>
{
    public async Task<ApiResponse<LoanApplicationDetailDto>> Handle(
        GetLoanApplicationByIdQuery request, 
        CancellationToken cancellationToken)
    {
        // Optimized read-only query with DTO projection
        var dto = await _context.LoanApplications
            .Where(x => x.Id == request.Id)
            .Select(x => new LoanApplicationDetailDto
            {
                Id = x.Id,
                Purpose = x.Purpose,
                Amount = x.RequestedAmount.Amount,
                Currency = x.RequestedAmount.Currency,
                Status = x.Status.ToString(),
                BorrowerName = x.Borrower.FullName,
                LoanProductTitle = x.LoanProduct.Title
            })
            .FirstOrDefaultAsync();

        if (dto is null)
            return ApiResponse<LoanApplicationDetailDto>.Failure("Application not found.");

        return ApiResponse<LoanApplicationDetailDto>.Success(dto);
    }
}
```

## 🎯 Domain-Driven Design Patterns

### 1. Aggregate Roots
```csharp
// LoanApplication is an aggregate root
public sealed class LoanApplication : AuditableEntity
{
    // Encapsulates business rules
    // Controls access to internal state
    // Maintains consistency boundaries
}
```

### 2. Value Objects
```csharp
// Money value object ensures type safety
public sealed class Money : IEquatable<Money>
{
    // Immutable
    // Self-validating
    // Equality based on value, not identity
}

// InterestRate value object
public sealed class InterestRate : IEquatable<InterestRate>
{
    public decimal Rate { get; }
    
    public static InterestRate Create(decimal rate)
    {
        if (rate < 0 || rate > 100)
            throw new DomainException("Interest rate must be between 0 and 100.");
        
        return new InterestRate(rate);
    }
}
```

### 3. Domain Services
```csharp
// Complex business logic that doesn't belong to a single entity
public sealed class LoanEligibilityService
{
    public bool IsEligible(Borrower borrower, LoanProduct product, Money requestedAmount)
    {
        // Complex eligibility rules
        if (borrower.CreditScore < product.MinimumCreditScore)
            return false;

        if (requestedAmount.Amount > product.MaximumAmount.Amount)
            return false;

        // Additional business rules...
        return true;
    }
}
```

## 🔧 Dependency Injection Configuration

### Application Layer Registration
```csharp
// LoanSuperMarket.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}
```

### Infrastructure Layer Registration
```csharp
// LoanSuperMarket.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IBorrowerRepository, BorrowerRepository>();
        services.AddScoped<ILoanProductRepository, LoanProductRepository>();

        return services;
    }
}
```

## 🧪 Testing Strategy

### Unit Testing Domain Logic
```csharp
[Test]
public void LoanApplication_Create_WithValidData_ShouldSucceed()
{
    // Arrange
    var borrowerId = Guid.NewGuid();
    var loanProductId = Guid.NewGuid();
    var amount = Money.Create(10000);
    var termMonths = 12;
    var purpose = "Home improvement";

    // Act
    var application = LoanApplication.Create(borrowerId, loanProductId, amount, termMonths, purpose);

    // Assert
    Assert.That(application.BorrowerId, Is.EqualTo(borrowerId));
    Assert.That(application.Status, Is.EqualTo(LoanApplicationStatus.Submitted));
}

[Test]
public void LoanApplication_Approve_WhenNotUnderReview_ShouldThrowDomainException()
{
    // Arrange
    var application = CreateValidLoanApplication();

    // Act & Assert
    Assert.Throws<DomainException>(() => application.Approve());
}
```

### Integration Testing API Endpoints
```csharp
[Test]
public async Task CreateLoanApplication_WithValidData_ShouldReturnCreated()
{
    // Arrange
    var command = new CreateLoanApplicationCommand
    {
        BorrowerId = _borrowerId,
        LoanProductId = _loanProductId,
        RequestedAmount = 10000,
        TermMonths = 12,
        Purpose = "Home improvement"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/loanapplications", command);

    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
}
```

This Clean Architecture implementation provides:
- ✅ **Testable business logic** in the domain layer
- ✅ **Scalable application services** with CQRS
- ✅ **Flexible data access** with repository pattern
- ✅ **Maintainable API layer** with proper separation
- ✅ **Rich domain models** with encapsulated behavior