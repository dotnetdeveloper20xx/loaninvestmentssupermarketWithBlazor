# 28 — SignalR Real-Time Notifications

## Overview

The platform uses SignalR to push real-time notifications from the server to connected Blazor clients. When a loan is funded, a payment is recorded, or the funding queue changes, connected users receive instant updates without polling. This document covers the server-side hub, client-side connection management, the abstraction layer, and CORS configuration.

---

## Feature Requirements (Plain English)

1. Notify lenders in real-time when the funding queue changes (new applications available).
2. Notify borrowers when their loan is funded.
3. Notify users when a payment is recorded on their loan.
4. Group connections by user ID and role for targeted notifications.
5. Start the SignalR connection after login, stop on logout.
6. Handle connection failures gracefully (app works without real-time).
7. Support automatic reconnection.

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Server Hub | ASP.NET Core SignalR | Hub with groups |
| Client | Microsoft.AspNetCore.SignalR.Client | HubConnection |
| Abstraction | IRealTimeNotifier interface | Dependency inversion |
| Auth | JWT Bearer over WebSocket | AccessTokenProvider |

---

## Server-Side: LoanHub

```csharp
// src/LoanSuperMarket.Api/Hubs/LoanHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LoanSuperMarket.Api.Hubs;

/// <summary>
/// SignalR hub for real-time loan platform notifications.
/// Clients join groups based on their role and user ID.
/// </summary>
[Authorize]
public sealed class LoanHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Each user gets their own group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        // Role-based groups
        var user = Context.User;
        if (user?.IsInRole("Lender") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "lenders");
        }

        if (user?.IsInRole("Borrower") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "borrowers");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

### Key Design Decisions

1. **`[Authorize]` attribute** — Only authenticated users can connect. The JWT token is validated on connection.
2. **User-specific groups** (`user-{userId}`) — Enables sending notifications to a specific user regardless of how many tabs/devices they have open.
3. **Role-based groups** (`lenders`, `borrowers`) — Enables broadcasting to all users of a role.
4. **No client-to-server methods** — This hub is server-push only. Clients don't invoke hub methods.

---

## Application Layer: IRealTimeNotifier Interface

```csharp
// src/LoanSuperMarket.Application/Common/Interfaces/IRealTimeNotifier.cs
namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Pushes real-time notifications to connected clients via SignalR.
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>Notifies all lenders that the funding queue has changed.</summary>
    Task NotifyFundingQueueChangedAsync(CancellationToken cancellationToken = default);

    /// <summary>Notifies a specific user that a payment was recorded on their loan.</summary>
    Task NotifyPaymentRecordedAsync(
        string userId, Guid scheduleId, decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>Notifies a specific user that their loan was funded.</summary>
    Task NotifyLoanFundedAsync(
        string borrowerUserId, Guid applicationId, decimal amount,
        CancellationToken cancellationToken = default);
}
```

**Why an interface?** The Application layer doesn't reference SignalR directly. This keeps it infrastructure-agnostic — you could swap SignalR for Azure SignalR Service, WebSockets, or even a message queue without changing application code.

---

## Infrastructure: SignalRNotifier Implementation

```csharp
// src/LoanSuperMarket.Api/Services/SignalRNotifier.cs
using LoanSuperMarket.Api.Hubs;
using LoanSuperMarket.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LoanSuperMarket.Api.Services;

public sealed class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<LoanHub> _hubContext;

    public SignalRNotifier(IHubContext<LoanHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyFundingQueueChangedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("lenders")
            .SendAsync("FundingQueueChanged", cancellationToken);
    }

    public async Task NotifyPaymentRecordedAsync(
        string userId, Guid scheduleId, decimal amount,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user-{userId}")
            .SendAsync("PaymentRecorded", new { scheduleId, amount }, cancellationToken);
    }

    public async Task NotifyLoanFundedAsync(
        string borrowerUserId, Guid applicationId, decimal amount,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user-{borrowerUserId}")
            .SendAsync("LoanFunded", new { applicationId, amount }, cancellationToken);
    }
}
```

### Usage in Command Handlers

```csharp
// In FundLoanCommandHandler.cs
public async Task<Unit> Handle(FundLoanCommand request, CancellationToken ct)
{
    // ... fund the loan ...

    // Notify the borrower in real-time
    await _realTimeNotifier.NotifyLoanFundedAsync(
        borrower.UserId, application.Id, application.RequestedAmount.Amount, ct);

    // Notify all lenders that the queue changed
    await _realTimeNotifier.NotifyFundingQueueChangedAsync(ct);

    return Unit.Value;
}
```

---

## DI Registration

```csharp
// API Program.cs
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

// Endpoint mapping
app.MapHub<LoanHub>("/hubs/loans");
```

---

## Client-Side: LoanHubClient

```csharp
// src/LoanSuperMarket.Blazor/Services/LoanHubClient.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoanSuperMarket.Blazor.Services;

public sealed class LoanHubClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    // Events that components can subscribe to
    public event Action? OnFundingQueueChanged;
    public event Action<Guid, decimal>? OnPaymentRecorded;
    public event Action<Guid, decimal>? OnLoanFunded;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public LoanHubClient(NavigationManager navigation)
    {
        var baseUri = navigation.BaseUri.TrimEnd('/');
        _hubUrl = $"{baseUri}/hubs/loans";
    }

    public async Task StartAsync(string? accessToken)
    {
        if (_connection is not null && _connection.State != HubConnectionState.Disconnected)
            return;

        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        // Register event handlers
        _connection.On("FundingQueueChanged", () =>
        {
            OnFundingQueueChanged?.Invoke();
        });

        _connection.On("PaymentRecorded", (Guid scheduleId, decimal amount) =>
        {
            OnPaymentRecorded?.Invoke(scheduleId, amount);
        });

        _connection.On("LoanFunded", (Guid applicationId, decimal amount) =>
        {
            OnLoanFunded?.Invoke(applicationId, amount);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            // Connection failed — non-critical, app works without real-time
        }
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.StopAsync(); }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.DisposeAsync(); }
            catch { }
            _connection = null;
        }
    }
}
```

---

## Connection Lifecycle

### Start on Login (MainLayout.razor)

```csharp
@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var token = await AuthProvider.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                await LoanHubClient.StartAsync(token);
            }
        }
    }
}
```

### Stop on Logout

```csharp
private async Task HandleLogoutAsync()
{
    await LoanHubClient.StopAsync();  // Disconnect SignalR
    await AuthApiClient.LogoutAsync(); // Clear tokens
    NavigationManager.NavigateTo("/login", forceLoad: false);
}
```

---

## Subscribing to Events in Components

```razor
@inject LoanHubClient LoanHubClient
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        LoanHubClient.OnFundingQueueChanged += HandleQueueChanged;
    }

    private async void HandleQueueChanged()
    {
        // Must use InvokeAsync to marshal back to the UI thread
        await InvokeAsync(async () =>
        {
            await RefreshDataAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        LoanHubClient.OnFundingQueueChanged -= HandleQueueChanged;
    }
}
```

---

## CORS Configuration for SignalR

SignalR uses WebSockets which require CORS to allow credentials:

```csharp
// API Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCorsPolicy", policy =>
    {
        policy
            .WithOrigins("https://localhost:5036", "http://localhost:5036")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// Must be applied before MapHub
app.UseCors("BlazorCorsPolicy");
app.MapHub<LoanHub>("/hubs/loans");
```

**Critical:** `.AllowCredentials()` is required for SignalR WebSocket connections. Without it, the browser blocks the upgrade request.

---

## JWT Authentication for SignalR

SignalR sends the JWT token as a query string parameter during the WebSocket handshake (browsers can't set headers on WebSocket connections). ASP.NET Core handles this automatically when you configure:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        // SignalR sends token as query string
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

---

## Adding a New Real-Time Event

### Step 1: Add method to IRealTimeNotifier

```csharp
Task NotifyApplicationStatusChangedAsync(
    string borrowerUserId, Guid applicationId, string newStatus,
    CancellationToken cancellationToken = default);
```

### Step 2: Implement in SignalRNotifier

```csharp
public async Task NotifyApplicationStatusChangedAsync(
    string borrowerUserId, Guid applicationId, string newStatus,
    CancellationToken cancellationToken = default)
{
    await _hubContext.Clients.Group($"user-{borrowerUserId}")
        .SendAsync("ApplicationStatusChanged",
            new { applicationId, newStatus }, cancellationToken);
}
```

### Step 3: Register handler in LoanHubClient

```csharp
_connection.On("ApplicationStatusChanged", (Guid appId, string status) =>
{
    OnApplicationStatusChanged?.Invoke(appId, status);
});
```

### Step 4: Call from command handler

```csharp
await _realTimeNotifier.NotifyApplicationStatusChangedAsync(
    borrower.UserId, application.Id, "Approved", ct);
```

### Step 5: Subscribe in component

```csharp
LoanHubClient.OnApplicationStatusChanged += (appId, status) =>
{
    InvokeAsync(() => { /* refresh UI */ StateHasChanged(); });
};
```

---

## Testing Considerations

- **Unit test SignalRNotifier:** Mock `IHubContext<LoanHub>`, verify `SendAsync` is called with correct group and event name.
- **Integration test:** Use `TestServer` with SignalR test client to verify end-to-end.
- **Graceful degradation:** Verify the app works when SignalR connection fails (all features should work, just without real-time updates).

---

## Common Pitfalls

1. **Thread marshaling** — SignalR callbacks run on a thread pool thread. Always use `InvokeAsync()` before calling `StateHasChanged()`.
2. **Memory leaks** — Always unsubscribe from events in `Dispose()`.
3. **Token expiry** — If the token expires while connected, the connection stays alive but new connections will fail. Consider reconnecting with a fresh token.
4. **CORS without credentials** — Forgetting `.AllowCredentials()` causes silent WebSocket failures.
5. **Hub URL mismatch** — The client constructs the URL from `NavigationManager.BaseUri`. In production with separate API/Blazor hosts, this needs configuration.
