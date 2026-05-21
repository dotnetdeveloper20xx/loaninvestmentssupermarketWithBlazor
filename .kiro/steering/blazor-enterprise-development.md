---
inclusion: manual
---

# Blazor Enterprise Development Steering Guide

## 🎯 Purpose

This steering guide provides comprehensive guidance for developing enterprise-grade Blazor applications with focus on scalability, maintainability, and performance. Use this when building complex business applications that need to scale to large teams and user bases.

## 🏗️ Component Architecture Patterns

### 1. Enterprise Component Base Classes

When creating reusable component infrastructure:

```csharp
// Base class for all enterprise components
public abstract class EnterpriseComponentBase<TModel> : ComponentBase, IDisposable
    where TModel : class, new()
{
    [Inject] protected ILogger Logger { get; set; } = null!;
    [Inject] protected ToastService ToastService { get; set; } = null!;
    [Inject] protected ApplicationStateService StateService { get; set; } = null!;

    protected TModel Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected List<string> ValidationErrors { get; set; } = new();

    protected virtual async Task<bool> ValidateAsync()
    {
        ValidationErrors.Clear();
        var validationResults = await ValidateModelAsync(Model);
        ValidationErrors.AddRange(validationResults);
        return !ValidationErrors.Any();
    }

    protected abstract Task<List<string>> ValidateModelAsync(TModel model);

    protected virtual async Task HandleErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Error in component {ComponentType}", GetType().Name);
        ErrorMessage = GetUserFriendlyErrorMessage(exception);
        ToastService.ShowError("Error", ErrorMessage);
        await InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose() { }
}
```

**Benefits**:
- Consistent error handling across all components
- Built-in validation framework
- Standardized loading states
- Proper resource cleanup

### 2. Smart Component Composition

For complex UI scenarios:

```csharp
// Composable data grid with enterprise features
public partial class EnterpriseDataGrid<TItem> : ComponentBase
{
    [Parameter] public IQueryable<TItem> Query { get; set; } = null!;
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = null!;
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public bool EnableVirtualization { get; set; }
    [Parameter] public EventCallback<TItem> OnItemSelected { get; set; }

    private GridState _gridState = new();
    private List<TItem> _items = new();
    private int _totalCount;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        
        try
        {
            var query = ApplyFiltersAndSorting(Query);
            _totalCount = await query.CountAsync();
            
            _items = await query
                .Skip((_gridState.Page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 3. Service-Oriented Architecture

For cross-cutting concerns:

```csharp
// Centralized application state management
public sealed class ApplicationStateService : IDisposable
{
    private readonly Dictionary<string, object> _state = new();
    private readonly Dictionary<string, List<Func<Task>>> _subscribers = new();

    public T? GetState<T>(string key) where T : class
    {
        return _state.TryGetValue(key, out var value) ? value as T : null;
    }

    public async Task SetStateAsync<T>(string key, T value) where T : class
    {
        _state[key] = value;
        await NotifySubscribersAsync(key);
    }

    public void Subscribe(string key, Func<Task> callback)
    {
        if (!_subscribers.ContainsKey(key))
            _subscribers[key] = new List<Func<Task>>();
        
        _subscribers[key].Add(callback);
    }

    private async Task NotifySubscribersAsync(string key)
    {
        if (!_subscribers.TryGetValue(key, out var callbacks)) return;

        var tasks = callbacks.Select(callback => callback());
        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _state.Clear();
        _subscribers.Clear();
    }
}
```

## 🎨 UI/UX Enterprise Patterns

### 1. Operational Dashboard Design

For business intelligence interfaces:

```razor
<!-- Enterprise dashboard layout -->
<div class="enterprise-dashboard">
    <div class="dashboard-header">
        <PageHeader Title="@Title" Subtitle="@Subtitle" />
        
        <div class="dashboard-actions">
            <button class="btn btn-primary" @onclick="RefreshData">
                <i class="icon-refresh"></i> Refresh
            </button>
            <button class="btn btn-secondary" @onclick="ExportData">
                <i class="icon-download"></i> Export
            </button>
        </div>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6 mb-8">
        @foreach (var metric in Metrics)
        {
            <MetricCard Title="@metric.Title"
                       Value="@metric.Value"
                       Change="@metric.Change"
                       Icon="@metric.Icon"
                       Trend="@metric.Trend" />
        }
    </div>

    <!-- Data Visualization -->
    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <AppCard Title="Recent Activity">
            <ActivityFeed Items="@RecentActivities" />
        </AppCard>
        
        <AppCard Title="Performance Trends">
            <ChartComponent Data="@ChartData" Type="ChartType.Line" />
        </AppCard>
    </div>
</div>
```

### 2. Workflow-Driven Interfaces

For operational business processes:

```razor
<!-- Workflow status component -->
<div class="workflow-container">
    <div class="workflow-header">
        <h3>@WorkflowTitle</h3>
        <StatusBadge Status="@CurrentStatus" />
    </div>

    <!-- Workflow steps -->
    <div class="workflow-steps">
        @foreach (var step in WorkflowSteps)
        {
            <div class="workflow-step @GetStepClass(step)">
                <div class="step-indicator">
                    @if (step.IsCompleted)
                    {
                        <i class="icon-check text-green-500"></i>
                    }
                    else if (step.IsActive)
                    {
                        <div class="loading loading-spinner loading-sm"></div>
                    }
                    else
                    {
                        <span class="step-number">@step.Order</span>
                    }
                </div>
                
                <div class="step-content">
                    <h4>@step.Title</h4>
                    <p>@step.Description</p>
                    
                    @if (step.IsActive && step.Actions.Any())
                    {
                        <div class="step-actions">
                            @foreach (var action in step.Actions)
                            {
                                <button class="btn @action.CssClass" 
                                       @onclick="() => ExecuteAction(action)">
                                    @action.Label
                                </button>
                            }
                        </div>
                    }
                </div>
            </div>
        }
    </div>
</div>
```

## 🔄 Data Flow & State Management

### 1. Command/Query Pattern Implementation

For complex business operations:

```csharp
// Command pattern for UI operations
public interface IUICommand<TResult>
{
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed class ProcessLoanApplicationCommand : IUICommand<ProcessingResult>
{
    private readonly LoanApplicationsApiClient _apiClient;
    private readonly Guid _applicationId;
    private readonly ProcessingAction _action;

    public ProcessLoanApplicationCommand(
        LoanApplicationsApiClient apiClient, 
        Guid applicationId, 
        ProcessingAction action)
    {
        _apiClient = apiClient;
        _applicationId = applicationId;
        _action = action;
    }

    public async Task<ProcessingResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _action switch
        {
            ProcessingAction.Approve => await _apiClient.ApproveAsync(_applicationId),
            ProcessingAction.Reject => await _apiClient.RejectAsync(_applicationId),
            ProcessingAction.RequestMoreInfo => await _apiClient.RequestMoreInfoAsync(_applicationId),
            _ => throw new InvalidOperationException($"Unknown action: {_action}")
        };
    }
}

// Usage in component
private async Task ProcessApplication(Guid applicationId, ProcessingAction action)
{
    var command = new ProcessLoanApplicationCommand(ApiClient, applicationId, action);
    
    try
    {
        var result = await command.ExecuteAsync();
        
        if (result.Success)
        {
            ToastService.ShowSuccess("Success", $"Application {action.ToString().ToLower()}d successfully.");
            await RefreshDataAsync();
        }
        else
        {
            ToastService.ShowError("Error", result.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        await HandleErrorAsync(ex);
    }
}
```

### 2. Reactive State Updates

For real-time data synchronization:

```csharp
// Real-time data service
public sealed class RealtimeDataService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly Dictionary<string, List<Func<object, Task>>> _handlers = new();

    public RealtimeDataService(NavigationManager navigationManager)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/operationshub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, object>("DataUpdated", OnDataUpdated);
    }

    public async Task StartAsync()
    {
        await _hubConnection.StartAsync();
    }

    public void Subscribe<T>(string dataType, Func<T, Task> handler)
    {
        if (!_handlers.ContainsKey(dataType))
            _handlers[dataType] = new List<Func<object, Task>>();

        _handlers[dataType].Add(data => handler((T)data));
    }

    private async Task OnDataUpdated(string dataType, object data)
    {
        if (!_handlers.TryGetValue(dataType, out var handlers)) return;

        var tasks = handlers.Select(handler => handler(data));
        await Task.WhenAll(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

// Usage in component
protected override async Task OnInitializedAsync()
{
    await RealtimeService.StartAsync();
    
    RealtimeService.Subscribe<LoanApplicationDto>("LoanApplication", async application =>
    {
        // Update local state
        var existingIndex = Applications.FindIndex(a => a.Id == application.Id);
        if (existingIndex >= 0)
        {
            Applications[existingIndex] = application;
        }
        else
        {
            Applications.Add(application);
        }
        
        await InvokeAsync(StateHasChanged);
        
        // Show notification
        ToastService.ShowInfo("Update", $"Application {application.Id} has been updated.");
    });
}
```

## 🚀 Performance Optimization Strategies

### 1. Virtual Scrolling for Large Datasets

```csharp
public partial class VirtualScrollList<TItem> : ComponentBase, IDisposable
{
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();
    [Parameter] public int ItemHeight { get; set; } = 50;
    [Parameter] public int ContainerHeight { get; set; } = 400;
    [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = null!;

    private ElementReference _containerRef;
    private int _scrollTop;
    private int _visibleStartIndex;
    private int _visibleEndIndex;

    protected override void OnParametersSet()
    {
        var visibleCount = (ContainerHeight / ItemHeight) + 2;
        _visibleStartIndex = Math.Max(0, (_scrollTop / ItemHeight) - 1);
        _visibleEndIndex = Math.Min(Items.Count - 1, _visibleStartIndex + visibleCount);
    }

    private async Task OnScroll()
    {
        _scrollTop = await _containerRef.GetScrollTopAsync();
        OnParametersSet();
        StateHasChanged();
    }
}
```

### 2. Efficient Component Rendering

```csharp
// Optimized component with ShouldRender override
public partial class OptimizedDataRow<TItem> : ComponentBase
{
    [Parameter] public TItem Item { get; set; } = default!;
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback<TItem> OnItemClicked { get; set; }

    private TItem? _previousItem;
    private bool _previousIsSelected;

    protected override bool ShouldRender()
    {
        var shouldRender = !EqualityComparer<TItem>.Default.Equals(_previousItem, Item) ||
                          _previousIsSelected != IsSelected;

        if (shouldRender)
        {
            _previousItem = Item;
            _previousIsSelected = IsSelected;
        }

        return shouldRender;
    }
}
```

## 🧪 Testing Enterprise Components

### 1. Component Testing Framework

```csharp
public abstract class EnterpriseComponentTestBase<TComponent> : TestContext
    where TComponent : IComponent
{
    protected Mock<ILogger<TComponent>> MockLogger { get; private set; } = null!;
    protected Mock<ToastService> MockToastService { get; private set; } = null!;
    protected Mock<ApplicationStateService> MockStateService { get; private set; } = null!;

    [SetUp]
    public virtual void Setup()
    {
        MockLogger = new Mock<ILogger<TComponent>>();
        MockToastService = new Mock<ToastService>();
        MockStateService = new Mock<ApplicationStateService>();

        Services.AddSingleton(MockLogger.Object);
        Services.AddSingleton(MockToastService.Object);
        Services.AddSingleton(MockStateService.Object);
    }

    protected IRenderedComponent<TComponent> RenderComponent(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
    {
        var component = RenderComponent<TComponent>(parameterBuilder);
        
        // Wait for component to stabilize
        component.WaitForState(() => !component.Markup.Contains("loading"), TimeSpan.FromSeconds(5));
        
        return component;
    }

    protected async Task SimulateUserInteractionAsync(
        IRenderedComponent<TComponent> component, 
        string selector, 
        string eventType = "click")
    {
        var element = component.Find(selector);
        await element.TriggerEventAsync(eventType, new EventArgs());
        component.Render();
    }
}
```

### 2. Integration Testing Patterns

```csharp
[TestFixture]
public class LoanApplicationWorkflowTests : EnterpriseComponentTestBase<LoanApplicationWorkflow>
{
    private Mock<LoanApplicationsApiClient> _mockApiClient = null!;

    protected override void Setup()
    {
        base.Setup();
        
        _mockApiClient = new Mock<LoanApplicationsApiClient>();
        Services.AddSingleton(_mockApiClient.Object);
    }

    [Test]
    public async Task ApproveApplication_WhenSuccessful_ShouldUpdateStatusAndShowNotification()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var application = CreateTestApplication(applicationId, LoanApplicationStatus.UnderReview);
        
        _mockApiClient
            .Setup(x => x.ApproveAsync(applicationId))
            .ReturnsAsync(ApiResponse.Success());

        var component = RenderComponent(parameters => parameters
            .Add(p => p.Application, application));

        // Act
        await SimulateUserInteractionAsync(component, "[data-testid='approve-button']");

        // Assert
        _mockApiClient.Verify(x => x.ApproveAsync(applicationId), Times.Once);
        
        MockToastService.Verify(
            x => x.ShowSuccess("Approved", "Application approved successfully."),
            Times.Once);
    }
}
```

## 🎯 Best Practices Summary

### Component Design
- Use base classes for common functionality
- Implement proper lifecycle management
- Optimize rendering with ShouldRender
- Handle errors gracefully at component level

### State Management
- Centralize application state
- Use reactive patterns for real-time updates
- Implement command/query separation
- Cache frequently accessed data

### Performance
- Implement virtual scrolling for large lists
- Use server-side paging and filtering
- Optimize component rendering cycles
- Minimize unnecessary re-renders

### Testing
- Test components in isolation
- Mock external dependencies
- Test user interactions
- Verify error handling scenarios

This steering guide ensures you build enterprise-grade Blazor applications that are scalable, maintainable, and performant.