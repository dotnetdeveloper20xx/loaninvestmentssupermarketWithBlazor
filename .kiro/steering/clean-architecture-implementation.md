---
inclusion: manual
---

# Clean Architecture Implementation Steering Guide

## 🎯 Purpose

This steering guide provides comprehensive guidance for implementing Clean Architecture in .NET applications with proper separation of concerns, dependency management, and testable design. Use this when building enterprise applications that need to be maintainable, scalable, and testable.

## 🏗️ Architecture Layer Implementation

### 1. Domain Layer (Core Business Logic)

The domain layer contains the business entities, value objects, and domain services.

#### Rich Domain Entities

```csharp
// Base entity with common properties
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    protected void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}

// Rich domain entity with business logic
public sealed class LoanApplication : AuditableEntity
{
    // Private constructor prevents invalid state
    private LoanApplication() 
    {
        Purpose = string.Empty;
        RequestedAmount = Money.Create(1);
    }

    private LoanApplication(
        Guid borrowerId,
        Guid loanProductId,
        Money requestedAmount,
        int termMonths,
        string purpose)
    {
        BorrowerId = borrowerId;
        LoanProductId = loanProductId;
        RequestedAmount = requestedAmount;
        TermMonths = termMonths;
        Purpose = purpose;
        Status = LoanApplicationStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public Guid BorrowerId { get; private set; }
    public Guid LoanProductId { get; private set; }
    public Money RequestedAmount { get; private set; }
    public int TermMonths { get; private set; }
    public string Purpose { get; private set; }
    public LoanApplicationStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }

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

        if (loanProductId == Guid.Empty)
            throw new DomainException("Loan product id is required.");

        if (requestedAmount.Amount <= 0)
            throw new DomainException("Requested amount must be greater than zero.");

        if (termMonths <= 0)
            throw new DomainException("Term must be greater than zero.");

        if (string.IsNullOrWhiteSpace(purpose))
            throw new DomainException("Loan purpose is required.");

        return new LoanApplication(borrowerId, loanProductId, requestedAmount, termMonths, purpose.Trim());
    }

    // Domain methods encapsulate business logic
    public void MarkUnderReview()
    {
        if (Status != LoanApplicationStatus.Submitted)
            throw new DomainException("Only submitted applications can move under review.");

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != LoanApplicationStatus.UnderReview)
            throw new DomainException("Only applications under review can be approved.");

        Status = LoanApplicationStatus.Approved;
        MarkUpdated();
    }

    public void Reject()
    {
        if (Status is LoanApplicationStatus.Approved or LoanApplicationStatus.Funded)
            throw new DomainException("Approved or funded applications cannot be rejected.");

        Status = LoanApplicationStatus.Rejected;
        MarkUpdated();
    }
}
```

#### Value Objects for Type Safety

```csharp
// Money value object
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

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add money with different currencies.");

        return Create(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot subtract money with different currencies.");

        return Create(Amount - other.Amount, Currency);
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public override string ToString() => $"{Currency} {Amount:N2}";

    public static bool operator ==(Money? left, Money? right) => 
        left?.Equals(right) ?? right is null;
    public static bool operator !=(Money? left, Money? right) => !(left == right);
}

// Interest rate value object
public sealed class InterestRate : IEquatable<InterestRate>
{
    private InterestRate(decimal rate)
    {
        Rate = rate;
    }

    public decimal Rate { get; }

    public static InterestRate Create(decimal rate)
    {
        if (rate < 0 || rate > 100)
            throw new DomainException("Interest rate must be between 0 and 100.");

        return new InterestRate(decimal.Round(rate, 4));
    }

    public decimal CalculateInterest(Money principal, int termMonths)
    {
        var monthlyRate = Rate / 100 / 12;
        return principal.Amount * monthlyRate * termMonths;
    }

    public bool Equals(InterestRate? other) => other is not null && Rate == other.Rate;
    public override bool Equals(object? obj) => obj is InterestRate other && Equals(other);
    public override int GetHashCode() => Rate.GetHashCode();
    public override string ToString() => $"{Rate:N4}%";
}
```

#### Domain Services for Complex Business Logic

```csharp
// Domain service for complex business rules
public sealed class LoanEligibilityService
{
    public EligibilityResult CheckEligibility(
        Borrower borrower, 
        LoanProduct product, 
        Money requestedAmount)
    {
        var reasons = new List<string>();

        // Credit score check
        if (borrower.CreditScore < product.MinimumCreditScore)
        {
            reasons.Add($"Credit score {borrower.CreditScore} is below minimum required {product.MinimumCreditScore}");
        }

        // Amount range check
        if (requestedAmount.Amount < product.MinimumAmount.Amount)
        {
            reasons.Add($"Requested amount is below minimum {product.MinimumAmount}");
        }

        if (requestedAmount.Amount > product.MaximumAmount.Amount)
        {
            reasons.Add($"Requested amount exceeds maximum {product.MaximumAmount}");
        }

        // Income verification
        var debtToIncomeRatio = CalculateDebtToIncomeRatio(borrower, requestedAmount);
        if (debtToIncomeRatio > 0.4m) // 40% maximum
        {
            reasons.Add($"Debt-to-income ratio {debtToIncomeRatio:P} exceeds maximum 40%");
        }

        return new EligibilityResult(reasons.Count == 0, reasons);
    }

    private decimal CalculateDebtToIncomeRatio(Borrower borrower, Money requestedAmount)
    {
        var monthlyIncome = borrower.AnnualIncome.Amount / 12;
        var estimatedMonthlyPayment = EstimateMonthlyPayment(requestedAmount, borrower.EstimatedInterestRate);
        var totalMonthlyDebt = borrower.ExistingMonthlyDebt.Amount + estimatedMonthlyPayment;

        return totalMonthlyDebt / monthlyIncome;
    }

    private decimal EstimateMonthlyPayment(Money loanAmount, InterestRate interestRate)
    {
        // Simplified calculation - in reality would be more complex
        var monthlyRate = interestRate.Rate / 100 / 12;
        var termMonths = 60; // Default term
        
        if (monthlyRate == 0) return loanAmount.Amount / termMonths;

        var factor = (decimal)Math.Pow((double)(1 + monthlyRate), termMonths);
        return loanAmount.Amount * monthlyRate * factor / (factor - 1);
    }
}

public sealed class EligibilityResult
{
    public EligibilityResult(bool isEligible, List<string> reasons)
    {
        IsEligible = isEligible;
        Reasons = reasons.AsReadOnly();
    }

    public bool IsEligible { get; }
    public IReadOnlyList<string> Reasons { get; }
}
```

### 2. Application Layer (Use Cases & Orchestration)

The application layer contains CQRS handlers, commands, queries, and application services.

#### Command/Query Implementation

```csharp
// Command for state changes
public sealed class CreateLoanApplicationCommand : IRequest<ApiResponse<Guid>>
{
    public Guid BorrowerId { get; set; }
    public Guid LoanProductId { get; set; }
    public decimal RequestedAmount { get; set; }
    public string Currency { get; set; } = "GBP";
    public int TermMonths { get; set; }
    public string Purpose { get; set; } = string.Empty;
}

// Command handler with business orchestration
public sealed class CreateLoanApplicationCommandHandler 
    : IRequestHandler<CreateLoanApplicationCommand, ApiResponse<Guid>>
{
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly LoanEligibilityService _eligibilityService;
    private readonly ILogger<CreateLoanApplicationCommandHandler> _logger;

    public CreateLoanApplicationCommandHandler(
        ILoanApplicationRepository loanApplicationRepository,
        IBorrowerRepository borrowerRepository,
        ILoanProductRepository loanProductRepository,
        LoanEligibilityService eligibilityService,
        ILogger<CreateLoanApplicationCommandHandler> logger)
    {
        _loanApplicationRepository = loanApplicationRepository;
        _borrowerRepository = borrowerRepository;
        _loanProductRepository = loanProductRepository;
        _eligibilityService = eligibilityService;
        _logger = logger;
    }

    public async Task<ApiResponse<Guid>> Handle(
        CreateLoanApplicationCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate entities exist
            var borrower = await _borrowerRepository.GetByIdAsync(request.BorrowerId);
            if (borrower is null)
            {
                _logger.LogWarning("Borrower {BorrowerId} not found", request.BorrowerId);
                return ApiResponse<Guid>.Failure("Borrower not found.");
            }

            var loanProduct = await _loanProductRepository.GetByIdAsync(request.LoanProductId);
            if (loanProduct is null)
            {
                _logger.LogWarning("Loan product {LoanProductId} not found", request.LoanProductId);
                return ApiResponse<Guid>.Failure("Loan product not found.");
            }

            // Check eligibility
            var requestedAmount = Money.Create(request.RequestedAmount, request.Currency);
            var eligibilityResult = _eligibilityService.CheckEligibility(borrower, loanProduct, requestedAmount);
            
            if (!eligibilityResult.IsEligible)
            {
                _logger.LogInformation("Loan application rejected for borrower {BorrowerId}: {Reasons}", 
                    request.BorrowerId, string.Join(", ", eligibilityResult.Reasons));
                
                return ApiResponse<Guid>.Failure("Application does not meet eligibility criteria.", 
                    eligibilityResult.Reasons.ToList());
            }

            // Create domain object
            var loanApplication = LoanApplication.Create(
                request.BorrowerId,
                request.LoanProductId,
                requestedAmount,
                request.TermMonths,
                request.Purpose);

            // Persist
            await _loanApplicationRepository.AddAsync(loanApplication);
            await _loanApplicationRepository.SaveChangesAsync();

            _logger.LogInformation("Loan application {ApplicationId} created for borrower {BorrowerId}", 
                loanApplication.Id, request.BorrowerId);

            return ApiResponse<Guid>.Success(loanApplication.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for loan application creation");
            return ApiResponse<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan application for borrower {BorrowerId}", request.BorrowerId);
            return ApiResponse<Guid>.Failure("An error occurred while creating the loan application.");
        }
    }
}

// Query for data retrieval
public sealed class GetLoanApplicationsPagedQuery : IRequest<ApiResponse<PagedResult<LoanApplicationDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public LoanApplicationStatus? StatusFilter { get; set; }
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

// Query handler with optimized data access
public sealed class GetLoanApplicationsPagedQueryHandler 
    : IRequestHandler<GetLoanApplicationsPagedQuery, ApiResponse<PagedResult<LoanApplicationDto>>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILogger<GetLoanApplicationsPagedQueryHandler> _logger;

    public async Task<ApiResponse<PagedResult<LoanApplicationDto>>> Handle(
        GetLoanApplicationsPagedQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var gridRequest = new GridQueryRequest
            {
                Page = request.Page,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortColumn = request.SortColumn,
                SortDirection = request.SortDirection
            };

            // Add status filter if provided
            if (request.StatusFilter.HasValue)
            {
                gridRequest.Filters.Add("Status", request.StatusFilter.Value.ToString());
            }

            var result = await _repository.GetPagedAsync(gridRequest);
            
            _logger.LogDebug("Retrieved {Count} loan applications (page {Page} of {TotalPages})", 
                result.Items.Count, result.CurrentPage, result.TotalPages);

            return ApiResponse<PagedResult<LoanApplicationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged loan applications");
            return ApiResponse<PagedResult<LoanApplicationDto>>.Failure(
                "An error occurred while retrieving loan applications.");
        }
    }
}
```

#### Repository Interfaces (Abstractions)

```csharp
// Base repository interface
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}

// Specific repository interface with domain-specific methods
public interface ILoanApplicationRepository : IRepository<LoanApplication>
{
    Task<PagedResult<LoanApplicationDto>> GetPagedAsync(GridQueryRequest request);
    Task<IEnumerable<LoanApplication>> GetByBorrowerIdAsync(Guid borrowerId);
    Task<IEnumerable<LoanApplication>> GetByStatusAsync(LoanApplicationStatus status);
    Task<int> GetCountByStatusAsync(LoanApplicationStatus status);
    Task<decimal> GetTotalFundedAmountAsync();
}
```

### 3. Infrastructure Layer (External Concerns)

The infrastructure layer implements repository interfaces and handles external concerns.

#### Repository Implementation

```csharp
// Base repository implementation
public abstract class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}

// Specific repository implementation
public sealed class LoanApplicationRepository : Repository<LoanApplication>, ILoanApplicationRepository
{
    public LoanApplicationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PagedResult<LoanApplicationDto>> GetPagedAsync(GridQueryRequest request)
    {
        var query = Context.LoanApplications
            .Include(x => x.Borrower)
            .Include(x => x.LoanProduct)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => 
                x.Purpose.Contains(request.SearchTerm) ||
                x.Borrower.FullName.Contains(request.SearchTerm) ||
                x.LoanProduct.Title.Contains(request.SearchTerm));
        }

        // Apply status filter
        if (request.Filters.TryGetValue("Status", out var statusValue) && 
            Enum.TryParse<LoanApplicationStatus>(statusValue, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        // Apply sorting
        query = request.SortColumn switch
        {
            "Purpose" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.Purpose)
                : query.OrderByDescending(x => x.Purpose),
            "Amount" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.RequestedAmount.Amount)
                : query.OrderByDescending(x => x.RequestedAmount.Amount),
            "Status" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.Status)
                : query.OrderByDescending(x => x.Status),
            "SubmittedAt" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.SubmittedAtUtc)
                : query.OrderByDescending(x => x.SubmittedAtUtc),
            _ => query.OrderByDescending(x => x.SubmittedAtUtc)
        };

        var totalCount = await query.CountAsync();

        // Project to DTO for performance
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LoanApplicationDto
            {
                Id = x.Id,
                Purpose = x.Purpose,
                Amount = x.RequestedAmount.Amount,
                Currency = x.RequestedAmount.Currency,
                TermMonths = x.TermMonths,
                Status = x.Status.ToString(),
                SubmittedAt = x.SubmittedAtUtc,
                BorrowerName = x.Borrower.FullName,
                LoanProductTitle = x.LoanProduct.Title
            })
            .ToListAsync();

        return new PagedResult<LoanApplicationDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<IEnumerable<LoanApplication>> GetByBorrowerIdAsync(Guid borrowerId)
    {
        return await DbSet
            .Where(x => x.BorrowerId == borrowerId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<LoanApplication>> GetByStatusAsync(LoanApplicationStatus status)
    {
        return await DbSet
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(LoanApplicationStatus status)
    {
        return await DbSet.CountAsync(x => x.Status == status);
    }

    public async Task<decimal> GetTotalFundedAmountAsync()
    {
        return await DbSet
            .Where(x => x.Status == LoanApplicationStatus.Funded)
            .SumAsync(x => x.RequestedAmount.Amount);
    }
}
```

#### Entity Framework Configuration

```csharp
// Entity configuration
public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.TermMonths)
            .IsRequired();

        // Value object mapping
        builder.OwnsOne(x => x.RequestedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("RequestedAmount")
                .HasPrecision(18, 2)
                .IsRequired();
            
            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Enum mapping
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.SubmittedAtUtc)
            .IsRequired();

        // Relationships
        builder.HasOne<Borrower>()
            .WithMany()
            .HasForeignKey(x => x.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LoanProduct>()
            .WithMany()
            .HasForeignKey(x => x.LoanProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.BorrowerId);
        builder.HasIndex(x => x.LoanProductId);
        builder.HasIndex(x => x.SubmittedAtUtc);
    }
}
```

### 4. Dependency Injection Configuration

```csharp
// Application layer registration
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Domain services
        services.AddScoped<LoanEligibilityService>();
        
        return services;
    }
}

// Infrastructure layer registration
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IBorrowerRepository, BorrowerRepository>();
        services.AddScoped<ILoanProductRepository, LoanProductRepository>();
        services.AddScoped<ILenderRepository, LenderRepository>();

        return services;
    }
}
```

## 🧪 Testing Strategy

### Unit Testing Domain Logic

```csharp
[TestFixture]
public class LoanApplicationTests
{
    [Test]
    public void Create_WithValidData_ShouldSucceed()
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
        Assert.That(application.LoanProductId, Is.EqualTo(loanProductId));
        Assert.That(application.RequestedAmount, Is.EqualTo(amount));
        Assert.That(application.TermMonths, Is.EqualTo(termMonths));
        Assert.That(application.Purpose, Is.EqualTo(purpose));
        Assert.That(application.Status, Is.EqualTo(LoanApplicationStatus.Submitted));
    }

    [Test]
    public void Approve_WhenNotUnderReview_ShouldThrowDomainException()
    {
        // Arrange
        var application = CreateValidLoanApplication();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => application.Approve());
        Assert.That(exception.Message, Is.EqualTo("Only applications under review can be approved."));
    }

    [Test]
    public void MarkUnderReview_WhenSubmitted_ShouldChangeStatus()
    {
        // Arrange
        var application = CreateValidLoanApplication();

        // Act
        application.MarkUnderReview();

        // Assert
        Assert.That(application.Status, Is.EqualTo(LoanApplicationStatus.UnderReview));
    }

    private static LoanApplication CreateValidLoanApplication()
    {
        return LoanApplication.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(10000),
            12,
            "Home improvement");
    }
}
```

### Integration Testing Application Layer

```csharp
[TestFixture]
public class CreateLoanApplicationCommandHandlerTests : IntegrationTestBase
{
    private CreateLoanApplicationCommandHandler _handler;
    private Mock<IBorrowerRepository> _mockBorrowerRepository;
    private Mock<ILoanProductRepository> _mockLoanProductRepository;
    private Mock<ILoanApplicationRepository> _mockLoanApplicationRepository;
    private Mock<LoanEligibilityService> _mockEligibilityService;

    [SetUp]
    public void Setup()
    {
        _mockBorrowerRepository = new Mock<IBorrowerRepository>();
        _mockLoanProductRepository = new Mock<ILoanProductRepository>();
        _mockLoanApplicationRepository = new Mock<ILoanApplicationRepository>();
        _mockEligibilityService = new Mock<LoanEligibilityService>();

        _handler = new CreateLoanApplicationCommandHandler(
            _mockLoanApplicationRepository.Object,
            _mockBorrowerRepository.Object,
            _mockLoanProductRepository.Object,
            _mockEligibilityService.Object,
            Mock.Of<ILogger<CreateLoanApplicationCommandHandler>>());
    }

    [Test]
    public async Task Handle_WithValidCommand_ShouldCreateLoanApplication()
    {
        // Arrange
        var borrower = CreateTestBorrower();
        var loanProduct = CreateTestLoanProduct();
        var command = new CreateLoanApplicationCommand
        {
            BorrowerId = borrower.Id,
            LoanProductId = loanProduct.Id,
            RequestedAmount = 10000,
            TermMonths = 12,
            Purpose = "Home improvement"
        };

        _mockBorrowerRepository
            .Setup(x => x.GetByIdAsync(borrower.Id))
            .ReturnsAsync(borrower);

        _mockLoanProductRepository
            .Setup(x => x.GetByIdAsync(loanProduct.Id))
            .ReturnsAsync(loanProduct);

        _mockEligibilityService
            .Setup(x => x.CheckEligibility(borrower, loanProduct, It.IsAny<Money>()))
            .Returns(new EligibilityResult(true, new List<string>()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.EqualTo(Guid.Empty));

        _mockLoanApplicationRepository.Verify(
            x => x.AddAsync(It.IsAny<LoanApplication>()), 
            Times.Once);
        
        _mockLoanApplicationRepository.Verify(
            x => x.SaveChangesAsync(), 
            Times.Once);
    }
}
```

This steering guide ensures you implement Clean Architecture correctly with proper separation of concerns, testable design, and maintainable code structure.