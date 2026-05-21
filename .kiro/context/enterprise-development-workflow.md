# Enterprise Development Workflow & Best Practices

## 🎯 Development Philosophy

This project demonstrates **enterprise-grade development practices** that scale to large teams and complex business requirements:

- **Feature-driven development** with proper planning
- **Test-driven development** for reliable code
- **Domain-first design** before implementation
- **Performance-conscious architecture** from day one
- **Maintainable code patterns** for long-term evolution

## 🚀 Development Environment Setup

### Prerequisites
```powershell
# Required tools
- .NET 10 SDK
- Node.js (for TailwindCSS)
- SQL Server or SQL Server Express
- Visual Studio 2022 or VS Code
- PowerShell 7+
```

### Quick Start Script
```powershell
# start-dev.ps1 - Automated development startup
Write-Host "Starting Loan SuperMarket Development Environment..." -ForegroundColor Green

# Start API
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd src/LoanSuperMarket.Api; dotnet run"

# Start Blazor frontend
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd src/LoanSuperMarket.Blazor; dotnet run"

# Start TailwindCSS watcher
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd src/LoanSuperMarket.Blazor; npm run watch"

Write-Host "All services started!" -ForegroundColor Green
Write-Host "API: https://localhost:7001" -ForegroundColor Yellow
Write-Host "Blazor: https://localhost:5036" -ForegroundColor Yellow
```

## 🏗️ Feature Development Workflow

### 1. Domain-First Development

#### Step 1: Define Domain Model
```csharp
// Start with rich domain entities
public sealed class LoanProduct : AuditableEntity
{
    // Private constructor prevents invalid state
    private LoanProduct() { }

    // Factory method with validation
    public static LoanProduct Create(
        string title,
        string description,
        Money minimumAmount,
        Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths,
        int maximumTermMonths,
        Guid lenderId)
    {
        // Domain validation
        ValidateInputs(title, description, minimumAmount, maximumAmount, ...);
        
        return new LoanProduct(title, description, ...);
    }

    // Domain methods encapsulate business logic
    public void SubmitForApproval()
    {
        if (Status != LoanProductStatus.Draft)
            throw new DomainException("Only draft loan products can be submitted for approval.");

        Status = LoanProductStatus.PendingApproval;
        MarkUpdated();
    }
}
```

#### Step 2: Define Value Objects
```csharp
// Type-safe value objects
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

### 2. Application Layer Development

#### Step 1: Define Commands and Queries
```csharp
// Command for state changes
public sealed class CreateLoanProductCommand : IRequest<ApiResponse<Guid>>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int MinimumTermMonths { get; set; }
    public int MaximumTermMonths { get; set; }
    public Guid LenderId { get; set; }
}

// Query for data retrieval
public sealed class GetLoanProductsPagedQuery : IRequest<ApiResponse<PagedResult<LoanProductDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public LoanProductStatus? StatusFilter { get; set; }
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; }
}
```

#### Step 2: Implement Handlers
```csharp
public sealed class CreateLoanProductCommandHandler 
    : IRequestHandler<CreateLoanProductCommand, ApiResponse<Guid>>
{
    private readonly ILoanProductRepository _repository;
    private readonly ILenderRepository _lenderRepository;

    public async Task<ApiResponse<Guid>> Handle(
        CreateLoanProductCommand request, 
        CancellationToken cancellationToken)
    {
        // Validation
        var lender = await _lenderRepository.GetByIdAsync(request.LenderId);
        if (lender is null)
            return ApiResponse<Guid>.Failure("Lender not found.");

        // Domain object creation
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

        // Persistence
        await _repository.AddAsync(loanProduct);
        await _repository.SaveChangesAsync();

        return ApiResponse<Guid>.Success(loanProduct.Id);
    }
}
```

### 3. Infrastructure Layer Development

#### Repository Implementation
```csharp
public sealed class LoanProductRepository : ILoanProductRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<PagedResult<LoanProductDto>> GetPagedAsync(GridQueryRequest request)
    {
        var query = _context.LoanProducts.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => 
                x.Title.Contains(request.SearchTerm) || 
                x.Description.Contains(request.SearchTerm));
        }

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == request.StatusFilter.Value);
        }

        // Apply sorting
        query = request.SortColumn switch
        {
            "Title" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.Title)
                : query.OrderByDescending(x => x.Title),
            "InterestRate" => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.InterestRate.Rate)
                : query.OrderByDescending(x => x.InterestRate.Rate),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync();

        // DTO projection for performance
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LoanProductDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                MinimumAmount = x.MinimumAmount.Amount,
                MaximumAmount = x.MaximumAmount.Amount,
                Currency = x.MinimumAmount.Currency,
                InterestRate = x.InterestRate.Rate,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAtUtc
            })
            .ToListAsync();

        return new PagedResult<LoanProductDto>(items, totalCount, request.Page, request.PageSize);
    }
}
```

### 4. API Layer Development

#### Controller Implementation
```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(CreateLoanProductCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanProductDto>>>> GetPaged(
        [FromQuery] GetLoanProductsPagedQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{id}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse>> SubmitForApproval(Guid id)
    {
        var command = new SubmitLoanProductForApprovalCommand { Id = id };
        var result = await _mediator.Send(command);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
```

### 5. Frontend Development

#### Typed API Client
```csharp
public sealed class LoanProductsApiClient
{
    private readonly HttpClient _httpClient;

    public async Task<ApiResponse<PagedResult<LoanProductDto>>> GetPagedAsync(GridQueryRequest request)
    {
        var queryString = BuildQueryString(request);
        var response = await _httpClient.GetAsync($"api/loanproducts/paged?{queryString}");
        
        if (!response.IsSuccessStatusCode)
            return ApiResponse<PagedResult<LoanProductDto>>.Failure("Failed to load loan products.");

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<LoanProductDto>>>(json, _jsonOptions);
        
        return result ?? ApiResponse<PagedResult<LoanProductDto>>.Failure("Invalid response format.");
    }

    public async Task<ApiResponse> SubmitForApprovalAsync(Guid id)
    {
        var response = await _httpClient.PutAsync($"api/loanproducts/{id}/submit-for-approval", null);
        
        if (!response.IsSuccessStatusCode)
            return ApiResponse.Failure("Failed to submit loan product for approval.");

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);
        
        return result ?? ApiResponse.Failure("Invalid response format.");
    }
}
```

#### Blazor Component
```razor
@page "/loan-products"
@inject LoanProductsApiClient LoanProductsApiClient
@inject ToastService ToastService
@inject ModalService ModalService

<PageHeader Title="Loan Products"
            Subtitle="Manage lending products and approval workflows" />

<AppCard>
    <DataGridToolbar SearchTerm="@_gridState.SearchTerm"
                     OnSearchChanged="HandleSearch"
                     OnCreateClicked="ShowCreateModal">
        <FilterContent>
            <select @onchange="HandleStatusFilter" class="select select-bordered select-sm">
                <option value="">All Statuses</option>
                <option value="Draft">Draft</option>
                <option value="PendingApproval">Pending Approval</option>
                <option value="Approved">Approved</option>
                <option value="Published">Published</option>
            </select>
        </FilterContent>
    </DataGridToolbar>

    <AppDataTable TItem="LoanProductDto"
                  Items="@_loanProducts"
                  IsLoading="@_isLoading"
                  EmptyMessage="No loan products found">
        <HeaderContent>
            <DataGridColumnHeader Column="Title"
                                  CurrentSort="@_gridState.SortColumn"
                                  CurrentDirection="@_gridState.SortDirection"
                                  OnSortChanged="HandleSort">
                Product
            </DataGridColumnHeader>
            <th>Amount Range</th>
            <th>Interest Rate</th>
            <th>Status</th>
            <th>Actions</th>
        </HeaderContent>
        <RowContent Context="product">
            <td class="font-medium">@product.Title</td>
            <td>£@product.MinimumAmount.ToString("N0") - £@product.MaximumAmount.ToString("N0")</td>
            <td>@product.InterestRate.ToString("N2")%</td>
            <td><StatusBadge Status="@product.Status" /></td>
            <td>
                @if (product.Status == "Draft")
                {
                    <button @onclick="() => SubmitForApproval(product.Id)"
                            class="btn btn-primary btn-sm">
                        Submit for Approval
                    </button>
                }
                else if (product.Status == "PendingApproval")
                {
                    <button @onclick="() => ApproveLoanProduct(product.Id)"
                            class="btn btn-success btn-sm">
                        Approve
                    </button>
                }
            </td>
        </RowContent>
    </AppDataTable>

    <DataGridPager CurrentPage="@_gridState.Page"
                   TotalPages="@_totalPages"
                   OnPageChanged="HandlePageChange" />
</AppCard>

@code {
    private List<LoanProductDto> _loanProducts = [];
    private GridState _gridState = new();
    private bool _isLoading = true;
    private int _totalPages;

    protected override async Task OnInitializedAsync()
    {
        await LoadLoanProductsAsync();
    }

    private async Task LoadLoanProductsAsync()
    {
        _isLoading = true;

        try
        {
            var request = new GridQueryRequest
            {
                Page = _gridState.Page,
                PageSize = _gridState.PageSize,
                SearchTerm = _gridState.SearchTerm,
                SortColumn = _gridState.SortColumn,
                SortDirection = _gridState.SortDirection
            };

            var response = await LoanProductsApiClient.GetPagedAsync(request);

            if (response.Success && response.Data is not null)
            {
                _loanProducts = response.Data.Items.ToList();
                _totalPages = response.Data.TotalPages;
            }
            else
            {
                ToastService.ShowError("Error", "Failed to load loan products.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Error", $"Failed to load loan products: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SubmitForApproval(Guid id)
    {
        var request = new ConfirmationModalRequest
        {
            Title = "Submit for Approval",
            Message = "This will submit the loan product for approval. Are you sure?",
            ConfirmText = "Submit",
            Intent = ModalIntent.Primary,
            OnConfirmAsync = async () =>
            {
                var result = await LoanProductsApiClient.SubmitForApprovalAsync(id);
                
                if (result.Success)
                {
                    ToastService.ShowSuccess("Submitted", "Loan product submitted for approval.");
                    await LoadLoanProductsAsync();
                }
                else
                {
                    ToastService.ShowError("Error", "Failed to submit loan product.");
                }
            }
        };

        ModalService.ShowConfirmation(request);
    }
}
```

## 🧪 Testing Strategy

### 1. Unit Testing Domain Logic
```csharp
[TestFixture]
public class LoanProductTests
{
    [Test]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var title = "Personal Loan";
        var description = "Flexible personal lending";
        var minimumAmount = Money.Create(1000);
        var maximumAmount = Money.Create(50000);
        var interestRate = InterestRate.Create(5.5m);
        var lenderId = Guid.NewGuid();

        // Act
        var loanProduct = LoanProduct.Create(
            title, description, minimumAmount, maximumAmount, 
            interestRate, 12, 60, lenderId);

        // Assert
        Assert.That(loanProduct.Title, Is.EqualTo(title));
        Assert.That(loanProduct.Status, Is.EqualTo(LoanProductStatus.Draft));
    }

    [Test]
    public void SubmitForApproval_WhenDraft_ShouldChangeStatus()
    {
        // Arrange
        var loanProduct = CreateValidLoanProduct();

        // Act
        loanProduct.SubmitForApproval();

        // Assert
        Assert.That(loanProduct.Status, Is.EqualTo(LoanProductStatus.PendingApproval));
    }

    [Test]
    public void SubmitForApproval_WhenNotDraft_ShouldThrowException()
    {
        // Arrange
        var loanProduct = CreateValidLoanProduct();
        loanProduct.SubmitForApproval(); // Now PendingApproval

        // Act & Assert
        Assert.Throws<DomainException>(() => loanProduct.SubmitForApproval());
    }
}
```

### 2. Integration Testing
```csharp
[TestFixture]
public class LoanProductsControllerTests : IntegrationTestBase
{
    [Test]
    public async Task CreateLoanProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var lender = await CreateTestLenderAsync();
        var command = new CreateLoanProductCommand
        {
            Title = "Test Loan Product",
            Description = "Test description",
            MinimumAmount = 1000,
            MaximumAmount = 50000,
            InterestRate = 5.5m,
            MinimumTermMonths = 12,
            MaximumTermMonths = 60,
            LenderId = lender.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/loanproducts", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<Guid>>(content);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.EqualTo(Guid.Empty));
    }
}
```

## 📊 Performance Optimization

### 1. Database Query Optimization
```csharp
// Efficient querying with projections
public async Task<PagedResult<LoanProductSummaryDto>> GetSummariesAsync(GridQueryRequest request)
{
    var query = _context.LoanProducts
        .Include(x => x.Lender) // Only include what's needed
        .AsQueryable();

    // Apply filters before projection
    query = ApplyFilters(query, request);

    var totalCount = await query.CountAsync();

    // Project to DTO to reduce data transfer
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(x => new LoanProductSummaryDto
        {
            Id = x.Id,
            Title = x.Title,
            InterestRate = x.InterestRate.Rate,
            Status = x.Status.ToString(),
            LenderName = x.Lender.CompanyName
        })
        .ToListAsync();

    return new PagedResult<LoanProductSummaryDto>(items, totalCount, request.Page, request.PageSize);
}
```

### 2. Frontend Performance
```razor
<!-- Use @key for efficient list rendering -->
@foreach (var product in loanProducts)
{
    <LoanProductRow @key="product.Id" Product="@product" />
}

<!-- Conditional rendering to avoid unnecessary components -->
@if (showExpensiveComponent)
{
    <ExpensiveComponent Data="@data" />
}

<!-- Virtualization for large lists -->
<Virtualize Items="@largeList" Context="item">
    <ItemTemplate>@item.Name</ItemTemplate>
</Virtualize>
```

This enterprise development workflow ensures:
- ✅ **Consistent code quality** across the team
- ✅ **Scalable architecture** that grows with requirements
- ✅ **Testable components** at every layer
- ✅ **Performance optimization** from the start
- ✅ **Maintainable codebase** for long-term evolution