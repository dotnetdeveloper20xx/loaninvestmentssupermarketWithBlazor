# 09 — Role-Based Authorization

## Overview

The Loan Investment Supermarket implements a **layered authorization system**:

1. **Role-based** — 6 predefined roles control broad access
2. **Policy-based** — Named policies map capabilities to role combinations
3. **Permission-based** — Granular module/action permissions for fine-grained control
4. **Frontend UI** — `AuthorizeView` components show/hide UI based on roles

---

## The 6 Predefined Roles

| Role | Description | Typical User |
|------|-------------|--------------|
| `Admin` | Full platform access including user management, system config, and disbursement approval | Platform owner |
| `CrmManager` | User vetting, registration approval, loan product approval, credit tier assignment, AML compliance | Operations staff |
| `CustomerService` | Dispute handling, messaging moderation, FAQ management, late payment cases | Support team |
| `Lender` | Create loan products, view own products, see applications against own products | Investors |
| `Borrower` | Apply for loans, view own applications, make payments | Loan applicants |
| `Auditor` | Read-only access to all platform data for compliance | Compliance officer |

### Role Seeding

Roles are seeded on application startup via `IdentitySeeder`:

**File:** `src/LoanSuperMarket.Infrastructure/Identity/IdentitySeeder.cs`

```csharp
private static readonly (string Name, string Description)[] PredefinedRoles =
[
    ("Admin", "Full platform access including user management, system configuration, and final loan disbursement approval"),
    ("CrmManager", "User vetting, registration approval/rejection, loan product approval, credit tier assignment, account limit management, and AML compliance"),
    ("CustomerService", "Handling disputes, messaging moderation, FAQ management, late payment cases, and mediation between borrowers and lenders"),
    ("Lender", "Create loan products, view own products, and see applications submitted against own products"),
    ("Borrower", "Apply for loans, view own applications, and make payments"),
    ("Auditor", "Read-only access to all platform data for compliance purposes")
];
```

Seeding is idempotent — it only creates roles that don't already exist:

```csharp
// Called in Program.cs after building the app
await IdentitySeeder.SeedAsync(app.Services);
```

---

## Named Authorization Policies

**File:** `src/LoanSuperMarket.Infrastructure/Identity/AuthorizationPolicies.cs`

```csharp
public static class AuthorizationPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanProcessApplications = nameof(CanProcessApplications);
    public const string CanManageProducts = nameof(CanManageProducts);
    public const string CanViewReports = nameof(CanViewReports);
    public const string CanManageLenders = nameof(CanManageLenders);
    public const string CanManageBorrowers = nameof(CanManageBorrowers);
    public const string CanVetUsers = nameof(CanVetUsers);
    public const string CanApproveProducts = nameof(CanApproveProducts);
    public const string CanHandleDisputes = nameof(CanHandleDisputes);
    public const string CanManageMessages = nameof(CanManageMessages);
    public const string CanSetLimits = nameof(CanSetLimits);
    public const string CanApproveDisbursements = nameof(CanApproveDisbursements);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageUsers, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(CanProcessApplications, policy =>
            policy.RequireRole("Admin", "CrmManager"));

        options.AddPolicy(CanManageProducts, policy =>
            policy.RequireRole("Admin", "CrmManager", "Lender"));

        options.AddPolicy(CanViewReports, policy =>
            policy.RequireRole("Admin", "Auditor"));

        options.AddPolicy(CanManageLenders, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(CanManageBorrowers, policy =>
            policy.RequireRole("Admin", "CrmManager"));

        options.AddPolicy(CanVetUsers, policy =>
            policy.RequireRole("CrmManager"));

        options.AddPolicy(CanApproveProducts, policy =>
            policy.RequireRole("CrmManager", "Admin"));

        options.AddPolicy(CanHandleDisputes, policy =>
            policy.RequireRole("CustomerService", "Admin"));

        options.AddPolicy(CanManageMessages, policy =>
            policy.RequireRole("CustomerService", "Admin"));

        options.AddPolicy(CanSetLimits, policy =>
            policy.RequireRole("CrmManager", "Admin"));

        options.AddPolicy(CanApproveDisbursements, policy =>
            policy.RequireRole("Admin"));
    }
}
```

### Policy-to-Role Mapping Table

| Policy | Admin | CrmManager | CustomerService | Lender | Borrower | Auditor |
|--------|:-----:|:----------:|:---------------:|:------:|:--------:|:-------:|
| CanManageUsers | ✅ | | | | | |
| CanProcessApplications | ✅ | ✅ | | | | |
| CanManageProducts | ✅ | ✅ | | ✅ | | |
| CanViewReports | ✅ | | | | | ✅ |
| CanManageLenders | ✅ | | | | | |
| CanManageBorrowers | ✅ | ✅ | | | | |
| CanVetUsers | | ✅ | | | | |
| CanApproveProducts | ✅ | ✅ | | | | |
| CanHandleDisputes | ✅ | | ✅ | | | |
| CanManageMessages | ✅ | | ✅ | | | |
| CanSetLimits | ✅ | ✅ | | | | |
| CanApproveDisbursements | ✅ | | | | | |

### Registration in Program.cs

```csharp
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.Configure(options);
});
```

---

## Using Authorization on Controllers

### [Authorize(Roles = "...")] — Direct Role Check

```csharp
[ApiController]
[Route("api/lenders")]
[Authorize(Roles = "Admin")]  // Only Admin can access all lender endpoints
public sealed class LendersController : ControllerBase
{
    // All endpoints in this controller require Admin role
}
```

Multiple roles (OR logic — any of these roles can access):

```csharp
[Authorize(Roles = "Admin,CrmManager,Lender")]
[HttpGet]
public async Task<ActionResult> GetLoanProducts() { ... }
```

### [Authorize(Policy = "...")] — Policy-Based Check

```csharp
[ApiController]
[Route("api/vetting")]
[Authorize(Policy = "CanVetUsers")]  // Only CrmManager (as defined in policy)
public sealed class VettingController : ControllerBase
{
    [HttpGet("queue")]
    public async Task<ActionResult> GetVettingQueue(...) { ... }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult> Approve(...) { ... }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult> Reject(...) { ... }
}
```

### When to Use Roles vs Policies

| Use Case | Approach | Example |
|----------|----------|---------|
| Single role check | `[Authorize(Roles = "Admin")]` | Admin-only endpoints |
| Multiple roles with business meaning | `[Authorize(Policy = "...")]` | "CanProcessApplications" = Admin OR CrmManager |
| Reusable across controllers | Policy | Same policy on multiple controllers |
| Quick one-off restriction | Roles | Single endpoint needs specific role |

**Best practice:** Use policies for anything that maps to a business capability. If the role mapping changes later, you only update `AuthorizationPolicies.cs`, not every controller.

---

## Permission Module/Action System

Beyond roles and policies, the system has a **granular permission system** for fine-grained access control.

### PermissionModule Enum

**File:** `src/LoanSuperMarket.Domain/Enums/PermissionModule.cs`

```csharp
public enum PermissionModule
{
    UserManagement,
    LoanManagement,
    ProductManagement,
    FinancialOperations,
    Reports,
    SystemSettings,
    Messaging
}
```

### PermissionAction Enum

**File:** `src/LoanSuperMarket.Domain/Enums/PermissionAction.cs`

```csharp
public enum PermissionAction
{
    View,
    Create,
    Edit,
    Delete,
    Approve
}
```

### RolePermission Entity

**File:** `src/LoanSuperMarket.Domain/Entities/Identity/RolePermission.cs`

```csharp
public sealed class RolePermission : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public PermissionModule Module { get; set; }
    public PermissionAction Action { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    // Navigation
    public CustomRole Role { get; set; } = null!;
}
```

### How Permissions Are Seeded

Each role gets specific module/action combinations:

```csharp
private static readonly Dictionary<string, (PermissionModule Module, PermissionAction Action)[]>
    RolePermissions = new()
{
    ["Admin"] = Enum.GetValues<PermissionModule>()
        .SelectMany(m => Enum.GetValues<PermissionAction>().Select(a => (m, a)))
        .ToArray(),  // Admin gets ALL permissions

    ["CrmManager"] =
    [
        (PermissionModule.UserManagement, PermissionAction.View),
        (PermissionModule.UserManagement, PermissionAction.Edit),
        (PermissionModule.UserManagement, PermissionAction.Approve),
        (PermissionModule.LoanManagement, PermissionAction.View),
        (PermissionModule.LoanManagement, PermissionAction.Approve),
        (PermissionModule.ProductManagement, PermissionAction.View),
        (PermissionModule.ProductManagement, PermissionAction.Approve),
        (PermissionModule.FinancialOperations, PermissionAction.View),
        (PermissionModule.FinancialOperations, PermissionAction.Edit),
    ],

    ["Lender"] =
    [
        (PermissionModule.ProductManagement, PermissionAction.View),
        (PermissionModule.ProductManagement, PermissionAction.Create),
        (PermissionModule.ProductManagement, PermissionAction.Edit),
        (PermissionModule.LoanManagement, PermissionAction.View),
    ],

    ["Borrower"] =
    [
        (PermissionModule.LoanManagement, PermissionAction.View),
        (PermissionModule.LoanManagement, PermissionAction.Create),
    ],

    ["Auditor"] =
    [
        (PermissionModule.UserManagement, PermissionAction.View),
        (PermissionModule.LoanManagement, PermissionAction.View),
        (PermissionModule.ProductManagement, PermissionAction.View),
        (PermissionModule.FinancialOperations, PermissionAction.View),
        (PermissionModule.Reports, PermissionAction.View),
        (PermissionModule.SystemSettings, PermissionAction.View),
        (PermissionModule.Messaging, PermissionAction.View),
    ]
};
```

### How Permissions Flow into the JWT

During token generation, permissions are loaded from the database and added as claims:

```csharp
// In JwtTokenService.GetUserPermissionsAsync()
var permissions = await _dbContext.RolePermissions
    .Where(rp => roleIds.Contains(rp.RoleId))
    .Select(rp => $"{rp.Module}.{rp.Action}")
    .Distinct()
    .ToListAsync(cancellationToken);

// Added to JWT as:
// "permissions": ["LoanManagement.View", "LoanManagement.Create", ...]
```

---

## CurrentUserService — Reading Claims from HttpContext

**File:** `src/LoanSuperMarket.Infrastructure/Identity/CurrentUserService.cs`

```csharp
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub");

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public IReadOnlyList<string> Roles =>
        (User?.FindAll("role")
            .Concat(User?.FindAll(ClaimTypes.Role) ?? [])
            .Select(c => c.Value)
            .Distinct()
            .ToList()
            .AsReadOnly()
        ?? (IReadOnlyList<string>)Array.Empty<string>());

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        User?.IsInRole(role) ?? false;

    public bool HasPermission(PermissionModule module, PermissionAction action)
    {
        var permissionClaim = $"{module}.{action}";

        return User?.FindAll("permissions")
            .Any(c => c.Value.Equals(permissionClaim,
                StringComparison.OrdinalIgnoreCase))
            ?? false;
    }
}
```

### Interface

```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(PermissionModule module, PermissionAction action);
}
```

### Usage in Command Handlers

```csharp
public sealed class SomeCommandHandler : IRequestHandler<SomeCommand, ApiResponse<string>>
{
    private readonly ICurrentUserService _currentUserService;

    public async Task<ApiResponse<string>> Handle(SomeCommand request, CancellationToken ct)
    {
        // Check who is making the request
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole("Admin");

        // Check granular permission
        if (!_currentUserService.HasPermission(
            PermissionModule.LoanManagement, PermissionAction.Approve))
        {
            return ApiResponse<string>.Fail("Insufficient permissions.");
        }

        // ... proceed with operation
    }
}
```

### Why Dual Claim Lookups?

```csharp
public string? UserId =>
    User?.FindFirstValue(ClaimTypes.NameIdentifier)  // Long URI format
    ?? User?.FindFirstValue("sub");                   // Short JWT format
```

Because `MapInboundClaims = false` means claims stay as short names (`"sub"`, `"email"`, `"role"`). But some middleware or libraries might still use the long URI format. The fallback ensures both work.

---

## How the Blazor Frontend Uses AuthorizeView

### Role-Based Navigation (MainLayout.razor)

```razor
@* Borrower-only navigation *@
<AuthorizeView Roles="Borrower">
    <Authorized>
        <NavLink href="wizard" Match="NavLinkMatch.Prefix">
            <span>Apply for Loan</span>
        </NavLink>
    </Authorized>
</AuthorizeView>

@* Admin and CrmManager *@
<AuthorizeView Roles="Admin,CrmManager">
    <Authorized>
        <NavLink href="loan-applications" Match="NavLinkMatch.Prefix">
            <span>Loan Applications</span>
        </NavLink>
    </Authorized>
</AuthorizeView>

@* CrmManager only *@
<AuthorizeView Roles="CrmManager">
    <Authorized>
        <NavLink href="vetting" Match="NavLinkMatch.Prefix">
            <span>Vetting Queue</span>
        </NavLink>
    </Authorized>
</AuthorizeView>

@* Admin only *@
<AuthorizeView Roles="Admin">
    <Authorized>
        <NavLink href="lenders" Match="NavLinkMatch.Prefix">
            <span>Lenders</span>
        </NavLink>
    </Authorized>
</AuthorizeView>
```

### Role-Based Dashboard (Home.razor)

```razor
<AuthorizeView>
    <Authorized>
        <AuthorizeView Roles="Admin,CrmManager,Auditor" Context="adminCtx">
            <Authorized>
                <AdminDashboardView />
            </Authorized>
        </AuthorizeView>

        <AuthorizeView Roles="Lender" Context="lenderCtx">
            <Authorized>
                <LenderDashboardView />
            </Authorized>
        </AuthorizeView>

        <AuthorizeView Roles="Borrower" Context="borrowerCtx">
            <Authorized>
                <BorrowerDashboardView />
            </Authorized>
        </AuthorizeView>
    </Authorized>

    <NotAuthorized>
        <LandingPage />
    </NotAuthorized>
</AuthorizeView>
```

### How AuthorizeView Works with JWT

1. `JwtAuthenticationStateProvider` parses the JWT access token
2. Extracts claims (including `"role"` claims)
3. Creates a `ClaimsPrincipal` that Blazor's authorization system uses
4. `AuthorizeView` checks the principal's roles against its `Roles` parameter
5. Renders `<Authorized>` or `<NotAuthorized>` content accordingly

### Important: Frontend Authorization is UI-Only

`AuthorizeView` only controls what the user **sees**. It does NOT prevent API access. A malicious user could still call the API directly. That's why:
- Controllers have `[Authorize]` attributes (server-side enforcement)
- The API validates the JWT on every request
- Frontend authorization is a UX convenience, not a security boundary

---

## Complete Authorization Stack

```
Request arrives at API
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  1. JWT Middleware (Authentication)                   │
│     - Validates token signature, expiry, issuer      │
│     - Creates ClaimsPrincipal from token claims      │
│     - Returns 401 if token is invalid/missing        │
└─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  2. [Authorize] Attribute (Authorization)            │
│     - Checks Roles or Policy requirements            │
│     - Returns 403 if user lacks required role        │
└─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  3. MediatR Pipeline Behaviours                      │
│     - AccountStatusBehaviour (account restrictions)  │
│     - ResourceAuthorizationBehaviour (data scoping)  │
│     - Returns domain-specific errors                 │
└─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  4. Command/Query Handler                            │
│     - May check ICurrentUserService.HasPermission()  │
│     - Business logic authorization                   │
└─────────────────────────────────────────────────────┘
```

---

## Adding a New Policy: Step-by-Step

1. **Add the constant** in `AuthorizationPolicies.cs`:
   ```csharp
   public const string CanManageNotifications = nameof(CanManageNotifications);
   ```

2. **Configure the policy** in the `Configure` method:
   ```csharp
   options.AddPolicy(CanManageNotifications, policy =>
       policy.RequireRole("Admin", "CustomerService"));
   ```

3. **Apply to controller**:
   ```csharp
   [Authorize(Policy = AuthorizationPolicies.CanManageNotifications)]
   public sealed class NotificationsController : ControllerBase { }
   ```

4. **Update Blazor navigation** (if needed):
   ```razor
   <AuthorizeView Roles="Admin,CustomerService">
       <Authorized>
           <NavLink href="notifications">Notifications</NavLink>
       </Authorized>
   </AuthorizeView>
   ```


---

## Advanced Patterns

### Pattern 1: Combining Role + Policy Authorization

```csharp
// Controller-level: require authentication
// Action-level: require specific policy
[Authorize]  // Must be authenticated
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() { ... }

    [Authorize(Policy = AuthorizationPolicies.CanApproveDisbursements)]
    [HttpPost("disbursements/{id}/approve")]
    public async Task<IActionResult> ApproveDisbursement(Guid id) { ... }
}
```

### Pattern 2: Programmatic Authorization in Handlers

Sometimes you need authorization logic that's more complex than what attributes support:

```csharp
public sealed class UpdateLoanProductCommandHandler
    : IRequestHandler<UpdateLoanProductCommand, ApiResponse<string>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILoanProductRepository _productRepo;

    public async Task<ApiResponse<string>> Handle(
        UpdateLoanProductCommand request, CancellationToken ct)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, ct);
        if (product is null)
            return ApiResponse<string>.Fail("Product not found.");

        // Admin can edit any product
        if (_currentUser.IsInRole("Admin"))
        {
            // proceed
        }
        // Lender can only edit their OWN products
        else if (_currentUser.IsInRole("Lender"))
        {
            if (product.LenderId != _currentUser.UserId)
                return ApiResponse<string>.Fail("You can only edit your own products.");
        }
        else
        {
            return ApiResponse<string>.Fail("Insufficient permissions.");
        }

        // ... update logic
    }
}
```

### Pattern 3: Permission-Based UI Rendering (Blazor)

```razor
@* In a Blazor component *@
@inject ICurrentUserService CurrentUser

<AuthorizeView Roles="Admin,CrmManager">
    <Authorized>
        <button @onclick="ApproveApplication">Approve</button>
    </Authorized>
</AuthorizeView>

@* Or using cascading auth state *@
<AuthorizeView>
    <Authorized>
        @if (context.User.IsInRole("Admin"))
        {
            <AdminPanel />
        }
        @if (context.User.HasClaim("permissions", "LoanManagement.Approve"))
        {
            <ApprovalButton />
        }
    </Authorized>
    <NotAuthorized>
        <p>Please log in to continue.</p>
    </NotAuthorized>
</AuthorizeView>
```

### Pattern 4: Custom Authorization Requirement (Future Extension)

If you need more complex policies beyond role checks:

```csharp
// Define a requirement
public class MinimumCreditTierRequirement : IAuthorizationRequirement
{
    public CreditTier MinimumTier { get; }
    public MinimumCreditTierRequirement(CreditTier minimumTier) => MinimumTier = minimumTier;
}

// Define a handler
public class MinimumCreditTierHandler
    : AuthorizationHandler<MinimumCreditTierRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumCreditTierRequirement requirement)
    {
        var tierClaim = context.User.FindFirst("credit_tier");
        if (tierClaim is not null
            && Enum.TryParse<CreditTier>(tierClaim.Value, out var tier)
            && tier >= requirement.MinimumTier)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.Configure(options);

    // Custom policy
    options.AddPolicy("PremiumBorrower", policy =>
        policy.Requirements.Add(new MinimumCreditTierRequirement(CreditTier.Gold)));
});

builder.Services.AddSingleton<IAuthorizationHandler, MinimumCreditTierHandler>();
```

---

## Troubleshooting Authorization

### Issue: 403 Forbidden but user has the correct role

**Possible causes:**
1. Role claim type mismatch — the token has `"role": "Admin"` but the identity expects
   `ClaimTypes.Role` (the long URI).
2. `MapInboundClaims` is `true` (default) — remaps claim types.
3. Missing `OnTokenValidated` event to fix the identity.

**Debug steps:**
```csharp
// Add temporary logging in a controller
[HttpGet("debug/claims")]
[Authorize]
public IActionResult GetClaims()
{
    var claims = User.Claims.Select(c => new { c.Type, c.Value });
    var roleClaimType = (User.Identity as ClaimsIdentity)?.RoleClaimType;
    return Ok(new { claims, roleClaimType });
}
```

### Issue: Policy always denies access

**Check:**
1. Is the policy registered? (`AuthorizationPolicies.Configure(options)` called?)
2. Is the role name exact? ("Admin" ≠ "admin" — case-sensitive!)
3. Is the user actually in that role? Check `AspNetUserRoles` table.

### Issue: Permissions not available in claims

**Check:**
1. Are permissions seeded? Check `RolePermissions` table.
2. Is the role linked to permissions? Check `RoleId` matches.
3. Is `GetUserPermissionsAsync` being called during token generation?

---

## Testing Authorization

```csharp
// Test that a policy correctly allows/denies access
[Fact]
public async Task VettingQueue_RequiresCrmManagerRole()
{
    // Arrange: create a token with "Borrower" role (should be denied)
    var borrowerToken = GenerateTestToken(roles: ["Borrower"]);

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", borrowerToken);

    // Act
    var response = await _client.GetAsync("/api/vetting/queue");

    // Assert: should be 403 Forbidden
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task VettingQueue_AllowsCrmManager()
{
    var crmToken = GenerateTestToken(roles: ["CrmManager"]);

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", crmToken);

    var response = await _client.GetAsync("/api/vetting/queue");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```
