# 27 — Reusable Component Library

## Overview

The Blazor client includes a library of reusable UI components that enforce consistent styling, reduce duplication, and accelerate feature development. Components live in `src/LoanSuperMarket.Blazor/Components/Common/` and are globally available via `_Imports.razor`.

---

## Feature Requirements (Plain English)

1. Consistent page headers with title, subtitle, and optional action button.
2. Loading skeletons that match the shape of the content they replace.
3. Empty state placeholders with icon, message, and optional CTA.
4. Status badges with color-coded variants.
5. A DataGrid system with sorting, pagination, and toolbar.
6. Modal system for confirmations and forms.
7. Drawer system for side panels.
8. Toast notifications for success/error/info/warning feedback.
9. Error boundary that catches unhandled exceptions gracefully.
10. Theme toggle for light/dark mode.

---

## Technologies & Patterns

| Pattern | Implementation |
|---------|---------------|
| Render Fragments | `RenderFragment` parameters for slot-based composition |
| Cascading Values | Theme state cascaded to all components |
| Service + Host | Services emit events, Host components subscribe and render |
| Parameters | `[Parameter]` for component configuration |

---

## 1. PageHeader

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/PageHeader.razor -->
<div class="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
    <div>
        <h1 class="text-2xl font-bold text-slate-900">@Title</h1>
        @if (!string.IsNullOrWhiteSpace(Subtitle))
        {
            <p class="mt-2 text-slate-600">@Subtitle</p>
        }
    </div>

    @if (ActionContent is not null)
    {
        <div>
            @ActionContent
        </div>
    }
</div>

@code {
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public RenderFragment? ActionContent { get; set; }
}
```

**Usage:**
```razor
<PageHeader Title="Loan Products" Subtitle="Manage available loan products.">
    <ActionContent>
        <button class="btn btn-primary" @onclick="OpenCreateModal">+ New Product</button>
    </ActionContent>
</PageHeader>
```

---

## 2. LoadingSkeleton

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/LoadingSkeleton.razor -->
<div class="animate-pulse space-y-4">
    @if (Variant == "cards")
    {
        <div class="grid grid-cols-2 gap-4 md:grid-cols-@Columns">
            @for (var i = 0; i < Columns; i++)
            {
                <div class="rounded-2xl border border-slate-200 bg-white p-5">
                    <div class="h-3 w-20 rounded bg-slate-200 mb-3"></div>
                    <div class="h-6 w-24 rounded bg-slate-200"></div>
                </div>
            }
        </div>
    }
    else if (Variant == "table")
    {
        <div class="rounded-2xl border border-slate-200 bg-white overflow-hidden">
            <div class="bg-slate-50 px-4 py-3 border-b border-slate-200">
                <div class="h-4 w-48 rounded bg-slate-200"></div>
            </div>
            @for (var i = 0; i < Rows; i++)
            {
                <div class="px-4 py-4 border-b border-slate-100 flex gap-4">
                    <div class="h-4 w-24 rounded bg-slate-200"></div>
                    <div class="h-4 w-32 rounded bg-slate-200"></div>
                    <div class="h-4 w-16 rounded bg-slate-200"></div>
                    <div class="h-4 flex-1 rounded bg-slate-200"></div>
                </div>
            }
        </div>
    }
    else
    {
        <div class="space-y-3">
            @for (var i = 0; i < Rows; i++)
            {
                <div class="h-4 w-@(i % 2 == 0 ? "3/4" : "1/2") rounded bg-slate-200"></div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public string Variant { get; set; } = "content"; // cards, table, content
    [Parameter] public int Rows { get; set; } = 5;
    [Parameter] public int Columns { get; set; } = 4;
}
```

---

## 3. EmptyState

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/EmptyState.razor -->
<div class="flex flex-col items-center justify-center py-16 text-center">
    <div class="text-5xl mb-4">@Icon</div>
    <h3 class="text-lg font-semibold text-slate-700">@Title</h3>
    <p class="mt-1 text-sm text-slate-500 max-w-sm">@Message</p>
    @if (ActionContent is not null)
    {
        <div class="mt-6">@ActionContent</div>
    }
</div>

@code {
    [Parameter] public string Icon { get; set; } = "📭";
    [Parameter] public string Title { get; set; } = "Nothing here yet";
    [Parameter] public string Message { get; set; } = "No data to display.";
    [Parameter] public RenderFragment? ActionContent { get; set; }
}
```

**Usage:**
```razor
<EmptyState Icon="🔍" Title="No results" Message="Try adjusting your filters.">
    <ActionContent>
        <button @onclick="ClearFilters" class="btn btn-outline">Clear Filters</button>
    </ActionContent>
</EmptyState>
```

---

## 4. Badge & StatusBadge

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/Badge.razor -->
<span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium @ColorClass">
    @Text
</span>

@code {
    [Parameter] public string Text { get; set; } = string.Empty;
    [Parameter] public string Color { get; set; } = "slate"; // blue, green, red, amber, purple

    private string ColorClass => Color switch
    {
        "blue" => "bg-blue-100 text-blue-700",
        "green" => "bg-green-100 text-green-700",
        "red" => "bg-red-100 text-red-700",
        "amber" => "bg-amber-100 text-amber-700",
        "purple" => "bg-purple-100 text-purple-700",
        "emerald" => "bg-emerald-100 text-emerald-700",
        _ => "bg-slate-100 text-slate-600"
    };
}
```

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/StatusBadge.razor -->
<span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium @GetClass()">
    @Status
</span>

@code {
    [Parameter] public string Status { get; set; } = string.Empty;

    private string GetClass() => Status.ToLowerInvariant() switch
    {
        "approved" or "performing" => "bg-green-100 text-green-700",
        "funded" or "active" => "bg-emerald-100 text-emerald-700",
        "submitted" or "pending" => "bg-blue-100 text-blue-700",
        "under review" => "bg-purple-100 text-purple-700",
        "rejected" or "defaulted" => "bg-red-100 text-red-700",
        "late" or "overdue" => "bg-amber-100 text-amber-700",
        "draft" or "closed" => "bg-slate-100 text-slate-600",
        _ => "bg-slate-100 text-slate-600"
    };
}
```

---

## 5. DataGrid System

The DataGrid is composed of three sub-components: Toolbar, ColumnHeader, and Pager.

### GridState Service

```csharp
// src/LoanSuperMarket.Blazor/Services/DataGrid/GridState.cs
namespace LoanSuperMarket.Blazor.Services.DataGrid;

public sealed class GridState
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortColumn { get; set; }
    public bool SortDescending { get; set; }
    public string? SearchTerm { get; set; }

    public int Skip => (CurrentPage - 1) * PageSize;

    public void SetSort(string column)
    {
        if (SortColumn == column)
            SortDescending = !SortDescending;
        else
        {
            SortColumn = column;
            SortDescending = false;
        }
        CurrentPage = 1; // Reset to first page on sort change
    }
}
```

### DataGridToolbar

```razor
<!-- src/LoanSuperMarket.Blazor/Components/DataGrid/DataGridToolbar.razor -->
<div class="flex items-center justify-between px-4 py-3 border-b border-slate-200">
    <div class="flex items-center gap-2">
        <input type="text" placeholder="Search..."
               value="@SearchTerm"
               @oninput="OnSearchChanged"
               class="rounded-lg border border-slate-300 px-3 py-1.5 text-sm w-64" />
    </div>
    @if (ActionContent is not null)
    {
        <div>@ActionContent</div>
    }
</div>

@code {
    [Parameter] public string? SearchTerm { get; set; }
    [Parameter] public EventCallback<string> SearchTermChanged { get; set; }
    [Parameter] public RenderFragment? ActionContent { get; set; }

    private async Task OnSearchChanged(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? "";
        await SearchTermChanged.InvokeAsync(value);
    }
}
```

### DataGridPager

```razor
<!-- src/LoanSuperMarket.Blazor/Components/DataGrid/DataGridPager.razor -->
<div class="flex items-center justify-between px-4 py-3 border-t border-slate-200">
    <span class="text-xs text-slate-500">
        Showing @(((CurrentPage - 1) * PageSize) + 1)–@Math.Min(CurrentPage * PageSize, TotalItems)
        of @TotalItems
    </span>
    <div class="flex gap-1">
        <button disabled="@(CurrentPage <= 1)" @onclick="PreviousPage"
                class="px-3 py-1 text-xs rounded border disabled:opacity-50">←</button>
        <button disabled="@(CurrentPage >= TotalPages)" @onclick="NextPage"
                class="px-3 py-1 text-xs rounded border disabled:opacity-50">→</button>
    </div>
</div>

@code {
    [Parameter] public int CurrentPage { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public int TotalItems { get; set; }
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }

    private int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    private async Task PreviousPage() => await CurrentPageChanged.InvokeAsync(CurrentPage - 1);
    private async Task NextPage() => await CurrentPageChanged.InvokeAsync(CurrentPage + 1);
}
```

---

## 6. Modal System

### ModalService

```csharp
// src/LoanSuperMarket.Blazor/Services/Modals/ModalService.cs
public sealed class ModalService
{
    public ConfirmationModalRequest? CurrentConfirmation { get; private set; }
    public event Action? OnChange;

    public void ShowConfirmation(ConfirmationModalRequest request)
    {
        CurrentConfirmation = request;
        NotifyStateChanged();
    }

    public void Close()
    {
        CurrentConfirmation = null;
        NotifyStateChanged();
    }

    public async Task ConfirmAsync()
    {
        if (CurrentConfirmation?.OnConfirmAsync is not null)
        {
            CurrentConfirmation.IsProcessing = true;
            NotifyStateChanged();
            await CurrentConfirmation.OnConfirmAsync();
        }
        Close();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### ConfirmationModalRequest

```csharp
public sealed class ConfirmationModalRequest
{
    public string Title { get; init; } = "Confirm";
    public string Message { get; init; } = "Are you sure?";
    public string ConfirmText { get; init; } = "Confirm";
    public ModalIntent Intent { get; init; } = ModalIntent.Primary;
    public Func<Task>? OnConfirmAsync { get; init; }
    public bool IsProcessing { get; set; }
}

public enum ModalIntent { Primary, Danger, Warning }
```

### ModalHost

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Modals/ModalHost.razor -->
@inject ModalService ModalService
@implements IDisposable

@if (ModalService.CurrentConfirmation is not null)
{
    var modal = ModalService.CurrentConfirmation;
    <div class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-black/50" @onclick="Close"></div>
        <div class="relative bg-white rounded-2xl shadow-xl p-6 max-w-md w-full mx-4">
            <h3 class="text-lg font-semibold text-slate-900">@modal.Title</h3>
            <p class="mt-2 text-sm text-slate-600">@modal.Message</p>
            <div class="mt-6 flex justify-end gap-3">
                <button @onclick="Close" class="px-4 py-2 text-sm rounded-lg border">Cancel</button>
                <button @onclick="Confirm" disabled="@modal.IsProcessing"
                        class="px-4 py-2 text-sm rounded-lg text-white @IntentClass(modal.Intent)">
                    @(modal.IsProcessing ? "Processing..." : modal.ConfirmText)
                </button>
            </div>
        </div>
    </div>
}

@code {
    protected override void OnInitialized() => ModalService.OnChange += StateHasChanged;
    public void Dispose() => ModalService.OnChange -= StateHasChanged;

    private void Close() => ModalService.Close();
    private async Task Confirm() => await ModalService.ConfirmAsync();

    private static string IntentClass(ModalIntent intent) => intent switch
    {
        ModalIntent.Danger => "bg-red-600 hover:bg-red-700",
        ModalIntent.Warning => "bg-amber-600 hover:bg-amber-700",
        _ => "bg-blue-600 hover:bg-blue-700"
    };
}
```

**Usage:**
```csharp
ModalService.ShowConfirmation(new ConfirmationModalRequest
{
    Title = "Delete Product",
    Message = "This action cannot be undone.",
    ConfirmText = "Delete",
    Intent = ModalIntent.Danger,
    OnConfirmAsync = async () => await DeleteProductAsync(productId)
});
```

---

## 7. Drawer System

### DrawerService

```csharp
public sealed class DrawerService
{
    public DrawerRequest? CurrentDrawer { get; private set; }
    public event Action? OnChange;

    public void Open(DrawerRequest request)
    {
        CurrentDrawer = request;
        NotifyStateChanged();
    }

    public async Task CloseAsync()
    {
        if (CurrentDrawer is null) return;
        CurrentDrawer.IsClosing = true;
        NotifyStateChanged();
        await Task.Delay(250); // Animation duration
        CurrentDrawer = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### DrawerRequest

```csharp
public sealed class DrawerRequest
{
    public string Title { get; init; } = string.Empty;
    public RenderFragment? Content { get; init; }
    public string Width { get; init; } = "max-w-md"; // Tailwind width class
    public bool IsClosing { get; set; }
}
```

### DrawerHost

```razor
@inject DrawerService DrawerService
@implements IDisposable

@if (DrawerService.CurrentDrawer is not null)
{
    var drawer = DrawerService.CurrentDrawer;
    <div class="fixed inset-0 z-40">
        <div class="absolute inset-0 bg-black/30" @onclick="CloseAsync"></div>
        <div class="absolute right-0 top-0 h-full @drawer.Width bg-white shadow-xl
                    transform transition-transform @(drawer.IsClosing ? "translate-x-full" : "translate-x-0")">
            <div class="flex items-center justify-between px-6 py-4 border-b">
                <h2 class="text-lg font-semibold">@drawer.Title</h2>
                <button @onclick="CloseAsync" class="text-slate-400 hover:text-slate-600">✕</button>
            </div>
            <div class="p-6 overflow-y-auto h-[calc(100%-4rem)]">
                @drawer.Content
            </div>
        </div>
    </div>
}

@code {
    protected override void OnInitialized() => DrawerService.OnChange += StateHasChanged;
    public void Dispose() => DrawerService.OnChange -= StateHasChanged;
    private async Task CloseAsync() => await DrawerService.CloseAsync();
}
```

---

## 8. Toast Notifications

### ToastService

```csharp
public sealed class ToastService
{
    private readonly List<ToastMessage> _messages = [];
    public IReadOnlyList<ToastMessage> Messages => _messages;
    public event Action? OnChange;

    public void ShowSuccess(string title, string message) => Add(ToastLevel.Success, title, message);
    public void ShowError(string title, string message) => Add(ToastLevel.Error, title, message);
    public void ShowInfo(string title, string message) => Add(ToastLevel.Info, title, message);
    public void ShowWarning(string title, string message) => Add(ToastLevel.Warning, title, message);

    public void Remove(Guid id) { /* remove and notify */ }

    private void Add(ToastLevel level, string title, string message)
    {
        var toast = new ToastMessage { Level = level, Title = title, Message = message };
        _messages.Add(toast);
        NotifyStateChanged();
        _ = AutoCloseAsync(toast.Id); // Fire-and-forget auto-dismiss after 4s
    }
}
```

### ToastContainer

```razor
@inject ToastService ToastService
@implements IDisposable

<div class="fixed bottom-4 right-4 z-50 space-y-2 max-w-sm">
    @foreach (var toast in ToastService.Messages)
    {
        <div class="rounded-xl shadow-lg border p-4 @LevelClass(toast.Level)
                    @(toast.IsClosing ? "opacity-0 translate-x-4" : "opacity-100")
                    transition-all duration-300">
            <div class="flex justify-between">
                <div>
                    <p class="text-sm font-semibold">@toast.Title</p>
                    <p class="text-xs mt-0.5">@toast.Message</p>
                </div>
                <button @onclick="() => ToastService.Remove(toast.Id)" class="text-xs">✕</button>
            </div>
        </div>
    }
</div>

@code {
    protected override void OnInitialized() => ToastService.OnChange += StateHasChanged;
    public void Dispose() => ToastService.OnChange -= StateHasChanged;

    private static string LevelClass(ToastLevel level) => level switch
    {
        ToastLevel.Success => "bg-green-50 border-green-200 text-green-800",
        ToastLevel.Error => "bg-red-50 border-red-200 text-red-800",
        ToastLevel.Warning => "bg-amber-50 border-amber-200 text-amber-800",
        _ => "bg-blue-50 border-blue-200 text-blue-800"
    };
}
```

---

## 9. AppErrorBoundary

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/AppErrorBoundary.razor -->
@inherits ErrorBoundary

@if (CurrentException is not null)
{
    <div class="rounded-2xl border border-red-200 bg-red-50 p-8 text-center">
        <div class="text-4xl mb-3">⚠️</div>
        <h2 class="text-lg font-semibold text-red-800">Something went wrong</h2>
        <p class="text-sm text-red-600 mt-2">
            An unexpected error occurred. Please try refreshing the page.
        </p>
        <button @onclick="Recover"
                class="mt-4 px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-medium hover:bg-red-700">
            Try Again
        </button>
    </div>
}
else
{
    @ChildContent
}
```

Used in `MainLayout.razor` to wrap `@Body`:
```razor
<AppErrorBoundary>
    @Body
</AppErrorBoundary>
```

---

## 10. ThemeToggle

```razor
<!-- src/LoanSuperMarket.Blazor/Components/Common/ThemeToggle.razor -->
@inject ThemeService ThemeService

<button @onclick="Toggle" class="btn btn-ghost btn-circle"
        title="@(ThemeService.IsDark ? "Switch to light mode" : "Switch to dark mode")">
    @(ThemeService.IsDark ? "☀️" : "🌙")
</button>

@code {
    private async Task Toggle()
    {
        await ThemeService.ToggleAsync();
        StateHasChanged();
    }
}
```

---

## How Components Connect in MainLayout

```
MainLayout.razor
├── <aside> Sidebar with role-based NavLinks
├── <header> with ThemeToggle, user info, logout
├── <main>
│   └── <AppErrorBoundary>
│       └── @Body (routed page content)
├── <ToastContainer /> — subscribes to ToastService.OnChange
├── <ModalHost /> — subscribes to ModalService.OnChange
└── <DrawerHost /> — subscribes to DrawerService.OnChange
```

---

## Step-by-Step Extension Guide

### Creating a new reusable component

1. Create file in `Components/Common/MyComponent.razor`
2. Define `[Parameter]` properties for configuration
3. Use `RenderFragment` for slot-based content
4. It's automatically available everywhere (via `_Imports.razor`)

### Adding a new toast level (e.g., "Neutral")

1. Add `Neutral` to `ToastLevel` enum
2. Add `ShowNeutral()` method to `ToastService`
3. Add color class in `ToastContainer`'s `LevelClass` switch

---

## Common Pitfalls

1. **Memory leaks** — Always implement `IDisposable` and unsubscribe from service events.
2. **StateHasChanged on disposed component** — Check `_disposed` flag before calling.
3. **Modal/Drawer stacking** — Current implementation supports one at a time. For stacking, use a `Stack<T>`.
