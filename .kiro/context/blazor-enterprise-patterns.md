# Blazor Enterprise Architecture Patterns & Best Practices

## 🎯 Enterprise Blazor Philosophy

This project demonstrates how to build **enterprise-grade Blazor applications** that rival Angular/React enterprise systems in terms of:

- **Scalable component architecture**
- **Reusable infrastructure patterns**
- **Operational SaaS UX design**
- **Performance-oriented data handling**
- **Maintainable large-scale UI systems**

## 🏗️ Component Architecture Excellence

### 1. Reusable UI Infrastructure

#### Global Service Layer
```csharp
// Centralized notification system
public sealed class ToastService
{
    public void ShowSuccess(string title, string message)
    public void ShowError(string title, string message)
    // Auto-dismiss with animations
}

// Modal orchestration
public sealed class ModalService
{
    public void ShowConfirmation(ConfirmationModalRequest request)
    // Async workflow handling
}

// Drawer system for quick views
public sealed class DrawerService
{
    public void ShowDrawer<T>(DrawerRequest<T> request)
    // Non-disruptive navigation
}
```

#### Component Composition Patterns
```razor
<!-- Reusable layout components -->
<PageHeader Title="Dashboard" 
            Subtitle="Operational analytics" />

<AppCard>
    <AppDataTable TItem="LoanApplicationDto"
                  Items="@applications"
                  IsLoading="@isLoading">
        <!-- Consistent table patterns -->
    </AppDataTable>
</AppCard>

<!-- Status rendering -->
<StatusBadge Status="@application.Status" />
```

### 2. Enterprise Form Architecture

#### Standardized Form Components
```razor
<!-- Consistent form controls -->
<FormSection Title="Loan Details">
    <AppTextInput @bind-Value="model.Title" 
                  Label="Product Title"
                  Required="true" />
    
    <AppNumberInput @bind-Value="model.Amount" 
                    Label="Loan Amount"
                    Prefix="£" />
    
    <AppDateInput @bind-Value="model.StartDate" 
                  Label="Start Date" />
</FormSection>

<FormActions>
    <button type="submit" class="btn btn-primary">
        Save Product
    </button>
</FormActions>
```

#### Benefits
- **Consistent styling** across all forms
- **Centralized validation** display
- **Reduced Tailwind duplication**
- **Maintainable form architecture**

### 3. Server-Side Data Grid Architecture

#### Enterprise DataGrid Pattern
```csharp
// Shared grid state management
public class GridState
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; }
}

// Server-side query contracts
public class GridQueryRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; }
}
```

#### Reusable Grid Components
```razor
<!-- Consistent toolbar -->
<DataGridToolbar SearchTerm="@gridState.SearchTerm"
                 OnSearchChanged="HandleSearch"
                 OnCreateClicked="ShowCreateModal" />

<!-- Sortable columns -->
<DataGridColumnHeader Column="Title"
                      CurrentSort="@gridState.SortColumn"
                      CurrentDirection="@gridState.SortDirection"
                      OnSortChanged="HandleSort" />

<!-- Consistent paging -->
<DataGridPager CurrentPage="@gridState.Page"
               TotalPages="@totalPages"
               OnPageChanged="HandlePageChange" />
```

## 🎨 Operational SaaS UX Patterns

### 1. Dashboard Architecture

#### KPI Card System
```razor
<!-- Reusable metric display -->
<div class="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-4">
    <MetricCard Title="Total Applications"
                Value="@dashboard.TotalApplications.ToString("N0")"
                Icon="📝" />
    
    <MetricCard Title="Approval Rate"
                Value="@($"{dashboard.ApprovalRate:N2}%")"
                Icon="📈" />
</div>
```

#### Activity Feed Patterns
```razor
<!-- Recent activity display -->
<div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
    <div class="border-b border-slate-200 px-6 py-4">
        <h3 class="text-lg font-bold text-slate-900">
            Recent Applications
        </h3>
    </div>
    
    <div class="overflow-x-auto">
        <!-- Consistent table styling -->
    </div>
</div>
```

### 2. Workflow-Driven UI

#### State-Based Actions
```razor
@if (loanProduct.Status == LoanProductStatus.Draft)
{
    <button @onclick="() => SubmitForApproval(loanProduct.Id)"
            class="btn btn-primary">
        Submit for Approval
    </button>
}
else if (loanProduct.Status == LoanProductStatus.PendingApproval)
{
    <button @onclick="() => ApproveLoanProduct(loanProduct.Id)"
            class="btn btn-success">
        Approve
    </button>
}
```

#### Confirmation Workflows
```csharp
private async Task ArchiveLoanProduct(Guid id)
{
    var request = new ConfirmationModalRequest
    {
        Title = "Archive Loan Product",
        Message = "This will remove the product from active listings.",
        ConfirmText = "Archive",
        Intent = ModalIntent.Warning,
        OnConfirmAsync = async () =>
        {
            await LoanProductsApiClient.ArchiveAsync(id);
            ToastService.ShowSuccess("Archived", "Loan product archived successfully.");
            await LoadLoanProductsAsync();
        }
    };

    ModalService.ShowConfirmation(request);
}
```

### 3. Quick-View Drawer System

#### Non-Disruptive Navigation
```razor
<!-- Drawer trigger -->
<button @onclick="() => ShowBorrowerDetails(borrower.Id)"
        class="text-blue-600 hover:text-blue-800">
    View Details
</button>

@code {
    private void ShowBorrowerDetails(Guid borrowerId)
    {
        var request = new DrawerRequest<Guid>
        {
            Title = "Borrower Details",
            Data = borrowerId,
            ComponentType = typeof(BorrowerDetailsDrawer)
        };

        DrawerService.ShowDrawer(request);
    }
}
```

## 🔄 Reactive UI Patterns

### 1. Event-Driven Communication

#### Component Communication
```csharp
// Parent component
[Parameter] public EventCallback<LoanApplicationDto> OnApplicationUpdated { get; set; }

// Child component
private async Task UpdateApplication()
{
    // Update logic
    await OnApplicationUpdated.InvokeAsync(updatedApplication);
}
```

#### Service-Based State Management
```csharp
// Global state service
public class ApplicationStateService
{
    public event Action<LoanApplicationDto>? ApplicationUpdated;
    
    public void NotifyApplicationUpdated(LoanApplicationDto application)
    {
        ApplicationUpdated?.Invoke(application);
    }
}
```

### 2. Async UI Handling

#### Loading States
```razor
@if (isLoading)
{
    <div class="flex items-center justify-center p-8">
        <div class="loading loading-spinner loading-lg"></div>
        <span class="ml-3">Loading applications...</span>
    </div>
}
else if (applications.Any())
{
    <!-- Data display -->
}
else
{
    <div class="text-center p-8 text-slate-500">
        No applications found
    </div>
}
```

#### Error Handling
```csharp
private async Task LoadDataAsync()
{
    isLoading = true;
    errorMessage = null;

    try
    {
        var response = await ApiClient.GetDataAsync();
        
        if (!response.Success)
        {
            errorMessage = string.Join(", ", response.Errors);
            return;
        }

        data = response.Data;
    }
    catch (Exception ex)
    {
        errorMessage = $"Failed to load data: {ex.Message}";
        ToastService.ShowError("Error", errorMessage);
    }
    finally
    {
        isLoading = false;
        StateHasChanged();
    }
}
```

## 🎯 Performance Optimization Patterns

### 1. Server-Side Data Operations

#### Efficient Querying
```csharp
// API endpoint with server-side operations
[HttpGet]
public async Task<ApiResponse<PagedResult<LoanApplicationDto>>> GetPaged(
    [FromQuery] GridQueryRequest request)
{
    var query = _context.LoanApplications.AsQueryable();

    // Server-side filtering
    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    {
        query = query.Where(x => x.Purpose.Contains(request.SearchTerm));
    }

    // Server-side sorting
    query = request.SortColumn switch
    {
        "Amount" => request.SortDirection == SortDirection.Ascending
            ? query.OrderBy(x => x.RequestedAmount.Amount)
            : query.OrderByDescending(x => x.RequestedAmount.Amount),
        _ => query.OrderByDescending(x => x.CreatedAtUtc)
    };

    // Server-side paging
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(x => new LoanApplicationDto
        {
            // DTO projection for performance
        })
        .ToListAsync();

    return ApiResponse<PagedResult<LoanApplicationDto>>.Success(
        new PagedResult<LoanApplicationDto>(items, totalCount, request.Page, request.PageSize));
}
```

### 2. Optimized Component Rendering

#### Conditional Rendering
```razor
@if (shouldRenderExpensiveComponent)
{
    <ExpensiveComponent Data="@data" />
}

<!-- Use @key for list performance -->
@foreach (var item in items)
{
    <ItemComponent @key="item.Id" Item="@item" />
}
```

## 🏢 Enterprise Styling Architecture

### 1. Consistent Design System

#### Tailwind + DaisyUI Integration
```css
/* Custom enterprise theme */
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Consistent component styling */
.enterprise-card {
    @apply rounded-2xl border border-slate-200 bg-white shadow-sm;
}

.enterprise-button-primary {
    @apply rounded-xl bg-blue-600 px-5 py-3 text-sm font-semibold text-white hover:bg-blue-700;
}
```

#### Component-Level Styling
```razor
<!-- Consistent enterprise styling -->
<div class="enterprise-card">
    <div class="border-b border-slate-200 px-6 py-4">
        <h3 class="text-lg font-bold text-slate-900">@Title</h3>
    </div>
    <div class="p-6">
        @ChildContent
    </div>
</div>
```

## 🚀 Scalability Patterns

### 1. Modular Architecture

#### Feature-Based Organization
```
Components/
├── Common/           # Shared UI components
├── Dashboard/        # Dashboard-specific components
├── LoanProducts/     # Loan product components
├── Borrowers/        # Borrower components
└── DataGrid/         # Reusable grid infrastructure

Services/
├── ApiClients/       # Typed HTTP clients
├── Notifications/    # Toast service
├── Modals/          # Modal orchestration
└── DataGrid/        # Grid state management
```

### 2. Dependency Injection Patterns

#### Service Registration
```csharp
// Program.cs - Clean service registration
builder.Services.AddScoped<LoanProductsApiClient>();
builder.Services.AddScoped<BorrowersApiClient>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ModalService>();
builder.Services.AddScoped<DrawerService>();
```

This architecture demonstrates **enterprise-grade Blazor development** that scales to large teams and complex business requirements while maintaining excellent developer experience and user experience.