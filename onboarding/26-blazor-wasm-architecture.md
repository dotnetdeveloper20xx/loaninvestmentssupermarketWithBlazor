# 26 — Blazor WebAssembly Architecture

## Overview

The Blazor WASM client is a standalone Single Page Application (SPA) that runs entirely in the browser via WebAssembly. It communicates with the ASP.NET Core API over HTTP, authenticates via JWT tokens stored in localStorage, and uses SignalR for real-time updates. This document covers the foundational architecture: Program.cs setup, authentication state management, HTTP token handling, routing, and layout.

---

## Feature Requirements (Plain English)

1. Bootstrap a Blazor WASM app with DI, auth, and HTTP client configuration.
2. Parse JWT tokens client-side to build ClaimsPrincipal (no server round-trip for auth state).
3. Automatically attach Bearer tokens to all API requests.
4. Handle 401 responses by refreshing the token or redirecting to login.
5. Monitor token expiration and proactively refresh before expiry.
6. Route authenticated users to role-appropriate pages; redirect unauthenticated users to login.
7. Provide a responsive layout with role-based sidebar navigation.

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| SPA Framework | Blazor WebAssembly (.NET 10) | Component-based UI |
| Auth State | Custom AuthenticationStateProvider | JWT client-side parsing |
| HTTP Auth | DelegatingHandler | Interceptor pattern |
| Token Storage | localStorage via JSInterop | Browser-native storage |
| Routing | Blazor Router | Attribute-based `@page` |
| Layout | MainLayout.razor | Shell pattern (sidebar + content) |
| Styling | Tailwind CSS + DaisyUI | Utility-first CSS |

---

## Program.cs — Application Bootstrap

```csharp
// src/LoanSuperMarket.Blazor/Program.cs
using LoanSuperMarket.Blazor;
using LoanSuperMarket.Blazor.Services;
using LoanSuperMarket.Blazor.Services.ApiClients;
using LoanSuperMarket.Blazor.Services.Auth;
using LoanSuperMarket.Blazor.Services.Drawers;
using LoanSuperMarket.Blazor.Services.Modals;
using LoanSuperMarket.Blazor.Services.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

// ─── Auth State Provider ───
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// ─── HTTP Pipeline ───
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

// ─── API Clients ───
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<LoanProductsApiClient>();
builder.Services.AddScoped<BorrowersApiClient>();
builder.Services.AddScoped<LendersApiClient>();
builder.Services.AddScoped<LoanApplicationsApiClient>();
builder.Services.AddScoped<WizardApiClient>();
builder.Services.AddScoped<ReviewQueueApiClient>();
builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<FundingApiClient>();
builder.Services.AddScoped<PaymentsApiClient>();

// ─── UI Services ───
builder.Services.AddScoped<LoanHubClient>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ModalService>();
builder.Services.AddScoped<DrawerService>();
builder.Services.AddScoped<WizardStateService>();

await builder.Build().RunAsync();
```

### Key Design Decisions

1. **Single HttpClient with DelegatingHandler** — All API calls go through one HttpClient that automatically attaches the Bearer token and handles 401 refresh.
2. **Scoped services** — In Blazor WASM, `AddScoped` behaves like a singleton per user session (there's only one DI scope per tab).
3. **Configuration from `wwwroot/appsettings.json`** — The `ApiBaseUrl` is read from the client-side config file, making it environment-configurable.

---

## JwtAuthenticationStateProvider

This is the heart of client-side authentication. It reads JWT tokens from localStorage, parses claims without any server call, and manages token lifecycle.

```csharp
// src/LoanSuperMarket.Blazor/Services/Auth/JwtAuthenticationStateProvider.cs
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IAsyncDisposable
{
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const int RefreshBufferMinutes = 2;
    private const int ExpiryCheckIntervalSeconds = 30;

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private Timer? _expiryTimer;

    // ─── Core Method: Get Authentication State ───
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await GetTokenFromStorageAsync(AccessTokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return CreateAnonymousState();

        var claims = ParseClaimsFromJwt(token);
        if (claims is null || claims.Length == 0)
            return CreateAnonymousState();

        // Check expiration — attempt refresh if expired
        if (IsTokenExpired(claims))
        {
            var refreshed = await TryRefreshTokenAsync();
            if (!refreshed)
            {
                await RemoveTokensFromStorageAsync();
                return CreateAnonymousState();
            }
            // Re-read after refresh
            token = await GetTokenFromStorageAsync(AccessTokenKey);
            claims = ParseClaimsFromJwt(token!);
        }

        StartExpiryMonitor(claims!);

        var identity = new ClaimsIdentity(claims!, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    // ─── Login: Store tokens and notify ───
    public async Task MarkUserAsAuthenticatedAsync(AuthTokenResponse tokenResponse)
    {
        await SetTokenInStorageAsync(AccessTokenKey, tokenResponse.AccessToken);
        await SetTokenInStorageAsync(RefreshTokenKey, tokenResponse.RefreshToken);

        var claims = ParseClaimsFromJwt(tokenResponse.AccessToken);
        StartExpiryMonitor(claims!);

        var identity = new ClaimsIdentity(claims!, "jwt");
        var state = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        NotifyAuthenticationStateChanged(state);
    }

    // ─── Logout: Clear tokens and notify ───
    public async Task MarkUserAsLoggedOutAsync()
    {
        StopExpiryMonitor();
        await RemoveTokensFromStorageAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAnonymousState()));
    }
}
```

### JWT Parsing (No External Library)

```csharp
private static Claim[]? ParseClaimsFromJwt(string jwt)
{
    var segments = jwt.Split('.');
    if (segments.Length != 3) return null;

    var payload = segments[1];
    var jsonBytes = ParseBase64WithoutPadding(payload);
    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

    var claims = new List<Claim>();
    foreach (var kvp in keyValuePairs!)
    {
        var claimType = MapJwtClaimType(kvp.Key);
        if (kvp.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in kvp.Value.EnumerateArray())
                claims.Add(new Claim(claimType, element.GetString() ?? ""));
        }
        else
        {
            claims.Add(new Claim(claimType, kvp.Value.ToString()));
        }
    }
    return claims.ToArray();
}

private static string MapJwtClaimType(string jwtClaimType) => jwtClaimType switch
{
    "sub" => ClaimTypes.NameIdentifier,
    "email" => ClaimTypes.Email,
    "given_name" => ClaimTypes.GivenName,
    "family_name" => ClaimTypes.Surname,
    "roles" or "role" => ClaimTypes.Role,
    _ => jwtClaimType
};
```

### Expiry Monitor

A `Timer` checks every 30 seconds if the token will expire within 2 minutes. If so, it proactively refreshes:

```csharp
private void StartExpiryMonitor(Claim[] claims)
{
    StopExpiryMonitor();
    _expiryTimer = new Timer(
        async _ => await CheckAndRefreshTokenAsync(),
        null,
        TimeSpan.FromSeconds(ExpiryCheckIntervalSeconds),
        TimeSpan.FromSeconds(ExpiryCheckIntervalSeconds));
}
```

---

## AuthTokenHandler — DelegatingHandler

This handler intercepts every HTTP request to:
1. Attach the Bearer token from localStorage.
2. Detect 401 responses and attempt a token refresh.
3. Retry the original request with the new token.
4. Redirect to login if refresh fails.

```csharp
// src/LoanSuperMarket.Blazor/Services/Auth/AuthTokenHandler.cs
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRefreshing;

    private static readonly string[] AuthEndpoints =
    [
        "api/auth/login",
        "api/auth/register",
        "api/auth/refresh-token",
        "api/auth/forgot-password",
        "api/auth/reset-password"
    ];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authStateProvider = _serviceProvider
            .GetRequiredService<JwtAuthenticationStateProvider>();

        // 1. Attach Bearer token
        var accessToken = await authStateProvider.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

        // 2. Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Handle 401 (skip for auth endpoints to avoid infinite loops)
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !_isRefreshing
            && !IsAuthEndpoint(request.RequestUri))
        {
            _isRefreshing = true;
            try
            {
                var refreshedResponse = await TryRefreshAndRetryAsync(
                    request, authStateProvider, cancellationToken);

                if (refreshedResponse is not null)
                    return refreshedResponse;

                // Refresh failed — logout and redirect
                await authStateProvider.MarkUserAsLoggedOutAsync();
                var nav = _serviceProvider.GetRequiredService<NavigationManager>();
                nav.NavigateTo("/login", forceLoad: true);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }
}
```

### Why `IServiceProvider` instead of direct injection?

The `DelegatingHandler` is created before the DI scope is fully resolved. Using `IServiceProvider` allows lazy resolution of scoped services like `JwtAuthenticationStateProvider`.

---

## AuthApiClient

```csharp
// src/LoanSuperMarket.Blazor/Services/Auth/AuthApiClient.cs
public sealed class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authProvider;

    public AuthApiClient(HttpClient httpClient, JwtAuthenticationStateProvider authProvider)
    {
        _httpClient = httpClient;
        _authProvider = authProvider;
    }

    public async Task<ApiResponse<AuthTokenResponse>?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();

        if (result?.Success == true && result.Data is not null)
            await _authProvider.MarkUserAsAuthenticatedAsync(result.Data);

        return result;
    }

    public async Task LogoutAsync()
    {
        await _authProvider.MarkUserAsLoggedOutAsync();
    }
}
```

---

## App.razor — Routing & Auth

```razor
<!-- src/LoanSuperMarket.Blazor/App.razor -->
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly" NotFoundPage="typeof(Pages.NotFound)">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <RedirectToLogin />
                    }
                    else
                    {
                        <RedirectToAccessDenied />
                    }
                </NotAuthorized>
                <Authorizing>
                    <div class="flex items-center justify-center min-h-screen">
                        <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent
                                    rounded-full animate-spin"></div>
                        <p class="text-sm text-slate-500">Checking authorization...</p>
                    </div>
                </Authorizing>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
    </Router>
</CascadingAuthenticationState>
```

### How routing works:

1. `CascadingAuthenticationState` provides auth context to all child components.
2. `AuthorizeRouteView` checks `[Authorize]` attributes on pages.
3. If not authenticated → `RedirectToLogin` navigates to `/login`.
4. If authenticated but wrong role → `RedirectToAccessDenied` navigates to `/access-denied`.
5. While checking → shows a spinner.

---

## MainLayout.razor — Shell Pattern

```razor
@inherits LayoutComponentBase
@inject NavigationManager NavigationManager
@inject AuthApiClient AuthApiClient
@inject LoanHubClient LoanHubClient
@inject JwtAuthenticationStateProvider AuthProvider

<div class="page-shell">
    <div class="flex min-h-screen">
        <!-- Sidebar (role-based navigation) -->
        <aside class="hidden lg:flex lg:w-72 lg:flex-col bg-[#071A2F] text-white">
            <div class="flex h-20 items-center px-6 border-b border-white/10">
                <div class="text-xl font-bold">Loan Super Market</div>
            </div>

            <nav class="flex-1 px-4 py-6 space-y-1 overflow-y-auto">
                <!-- Dashboard - all authenticated -->
                <AuthorizeView>
                    <Authorized>
                        <NavLink href="" Match="NavLinkMatch.All" ...>Dashboard</NavLink>
                    </Authorized>
                </AuthorizeView>

                <!-- Borrower-only links -->
                <AuthorizeView Roles="Borrower">
                    <Authorized>
                        <NavLink href="wizard">Apply for Loan</NavLink>
                        <NavLink href="borrower/dashboard">My Applications</NavLink>
                    </Authorized>
                </AuthorizeView>

                <!-- Lender links -->
                <AuthorizeView Roles="Lender,Admin">
                    <Authorized>
                        <NavLink href="funding-queue">Funding Queue</NavLink>
                        <NavLink href="lender-dashboard">My Portfolio</NavLink>
                    </Authorized>
                </AuthorizeView>

                <!-- Admin section -->
                <AuthorizeView Roles="Admin">
                    <Authorized>
                        <NavLink href="admin/users">User Management</NavLink>
                        <NavLink href="admin/roles">Roles</NavLink>
                    </Authorized>
                </AuthorizeView>
            </nav>
        </aside>

        <!-- Main content area -->
        <div class="flex min-w-0 flex-1 flex-col">
            <header class="h-20 border-b border-slate-200 bg-white px-6 flex items-center justify-between">
                <!-- User info, theme toggle, logout button -->
            </header>

            <main class="flex-1 p-6 lg:p-8">
                <AppErrorBoundary>
                    @Body
                </AppErrorBoundary>
            </main>

            <!-- Global UI hosts -->
            <ToastContainer />
            <ModalHost />
            <DrawerHost />
        </div>
    </div>
</div>

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Start SignalR connection after first render
            var token = await AuthProvider.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                await LoanHubClient.StartAsync(token);
        }
    }

    private async Task HandleLogoutAsync()
    {
        await LoanHubClient.StopAsync();
        await AuthApiClient.LogoutAsync();
        NavigationManager.NavigateTo("/login", forceLoad: false);
    }
}
```

### Role-Based Navigation Pattern

The `<AuthorizeView Roles="...">` component conditionally renders nav links based on the user's JWT claims. This means:
- Borrowers see: Apply, My Applications, My Loans
- Lenders see: Funding Queue, My Portfolio
- Admins see: Everything + Administration section

---

## _Imports.razor — Global Usings

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using LoanSuperMarket.Blazor
@using LoanSuperMarket.Blazor.Layout
@using LoanSuperMarket.Blazor.Components.Common
@using LoanSuperMarket.Blazor.Components.Dashboard
@using LoanSuperMarket.Shared.Dashboard
@using LoanSuperMarket.Shared.Common
@using LoanSuperMarket.Blazor.Services.ApiClients
@using LoanSuperMarket.Blazor.Services.Auth
@using LoanSuperMarket.Blazor.Services
@using LoanSuperMarket.Blazor.Services.Notifications
@using LoanSuperMarket.Blazor.Services.Modals
@using LoanSuperMarket.Blazor.Services.Drawers
@using LoanSuperMarket.Blazor.Components.Modals
@using LoanSuperMarket.Blazor.Components.Drawers
@using LoanSuperMarket.Blazor.Components.Notifications
```

This eliminates repetitive `@using` directives in every `.razor` file.

---

## Configuration — wwwroot/appsettings.json

```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

In production, this would point to the deployed API URL.

---

## Authentication Flow Diagram

```
1. User enters credentials on Login.razor
2. AuthApiClient.LoginAsync() → POST /api/auth/login
3. API returns { accessToken, refreshToken, expiresAt }
4. JwtAuthenticationStateProvider.MarkUserAsAuthenticatedAsync()
   a. Stores tokens in localStorage
   b. Parses claims from JWT payload
   c. Starts expiry monitor timer
   d. Calls NotifyAuthenticationStateChanged()
5. CascadingAuthenticationState propagates new state
6. AuthorizeRouteView re-evaluates → renders authorized content
7. MainLayout.OnAfterRenderAsync starts SignalR connection
```

---

## Step-by-Step Extension Guide

### Adding a new API client

1. Create `src/LoanSuperMarket.Blazor/Services/ApiClients/MyFeatureApiClient.cs`
2. Inject `HttpClient` in constructor
3. Add methods that call your API endpoints
4. Register in `Program.cs`: `builder.Services.AddScoped<MyFeatureApiClient>();`
5. Inject in your component: `@inject MyFeatureApiClient MyApi`

### Adding a new page

1. Create `src/LoanSuperMarket.Blazor/Pages/MyPage.razor`
2. Add `@page "/my-route"` directive
3. Add `@attribute [Authorize(Roles = "Admin")]` for role restriction
4. Add NavLink in `MainLayout.razor` inside appropriate `<AuthorizeView>`

### Adding a new role to navigation

Wrap the NavLink in `<AuthorizeView Roles="NewRole">`:
```razor
<AuthorizeView Roles="NewRole">
    <Authorized>
        <NavLink href="new-feature">New Feature</NavLink>
    </Authorized>
</AuthorizeView>
```

---

## Common Pitfalls

1. **JSInterop before render** — `localStorage` calls fail during server prerendering. The provider handles this with try/catch.
2. **Circular DI** — `AuthTokenHandler` uses `IServiceProvider` to avoid circular dependency with `JwtAuthenticationStateProvider`.
3. **Token refresh loops** — `AuthEndpoints` array prevents 401 interception on auth endpoints.
4. **Stale auth state** — Always call `NotifyAuthenticationStateChanged()` after token changes.
