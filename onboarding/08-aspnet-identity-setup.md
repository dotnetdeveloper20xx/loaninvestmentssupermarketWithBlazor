# 08 — ASP.NET Identity Setup

## Overview

The Loan Investment Supermarket uses **ASP.NET Core Identity** for user management, password hashing, lockout, email confirmation, and role assignment. However, it's configured with `AddIdentityCore` (not `AddIdentity`) to avoid conflicts with JWT Bearer authentication.

Identity runs on its own dedicated `AuthIdentityDbContext`, separate from the main `ApplicationDbContext` that handles domain entities (loans, products, etc.).

---

## Two-DbContext Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  SQL Server: LoanSuperMarketDb                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  AuthIdentityDbContext                ApplicationDbContext         │
│  ┌───────────────────┐               ┌───────────────────┐      │
│  │ AspNetUsers        │               │ LoanApplications   │      │
│  │ AspNetRoles        │               │ LoanProducts       │      │
│  │ AspNetUserRoles    │               │ Installments       │      │
│  │ RefreshTokens      │               │ Borrowers          │      │
│  │ UserSessions       │               │ Lenders            │      │
│  │ RolePermissions    │               │ AuditLogs          │      │
│  │ RecoveryCodes      │               │ ...                │      │
│  └───────────────────┘               └───────────────────┘      │
│                                                                   │
│  Same database, different DbContext instances                     │
└─────────────────────────────────────────────────────────────────┘
```

**Why two DbContexts?**
- **Separation of concerns:** Identity tables are managed by ASP.NET Identity; domain tables by our own migrations
- **Independent evolution:** Identity schema changes don't affect domain migrations and vice versa
- **Security boundary:** Identity operations go through UserManager/SignInManager; domain operations go through repositories

---

## AddIdentityCore Configuration

**File:** `src/LoanSuperMarket.Infrastructure/DependencyInjection.cs`

```csharp
services.AddIdentityCore<ApplicationUser>(options =>
{
    // Password policies
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // Lockout settings
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<CustomRole>()
.AddEntityFrameworkStores<AuthIdentityDbContext>()
.AddDefaultTokenProviders()
.AddSignInManager();
```

### Configuration Breakdown

| Category | Setting | Value | Purpose |
|----------|---------|-------|---------|
| Password | RequiredLength | 8 | Minimum password length |
| Password | RequireUppercase | true | At least one uppercase letter |
| Password | RequireLowercase | true | At least one lowercase letter |
| Password | RequireDigit | true | At least one number |
| Password | RequireNonAlphanumeric | true | At least one special character |
| Lockout | MaxFailedAccessAttempts | 5 | Lock after 5 failed logins |
| Lockout | DefaultLockoutTimeSpan | 15 min | How long the lockout lasts |
| Lockout | AllowedForNewUsers | true | New accounts can be locked out |
| User | RequireUniqueEmail | true | No duplicate email addresses |

### Why Each Chained Call Matters

```csharp
.AddRoles<CustomRole>()           // Registers RoleManager<CustomRole>
.AddEntityFrameworkStores<AuthIdentityDbContext>()  // Uses EF Core for storage
.AddDefaultTokenProviders()       // Email confirmation + password reset tokens
.AddSignInManager()               // Registers SignInManager (not included by default with AddIdentityCore)
```

---

## ApplicationUser Entity

**File:** `src/LoanSuperMarket.Domain/Entities/Identity/ApplicationUser.cs`

```csharp
using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingApproval;
    public string? AccountStatusReason { get; set; }
    public DateTime? AccountStatusChangedAtUtc { get; set; }
    public string? AccountStatusChangedBy { get; set; }
    public CreditTier? CreditTier { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? CapitalLimit { get; set; }
    public string? BlockedActivity { get; set; } // "Borrowing", "Lending", "Both"
    public bool TwoFactorSetupComplete { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserSession> Sessions { get; set; } = [];
}
```

### Custom Properties Explained

| Property | Type | Purpose |
|----------|------|---------|
| `FirstName` / `LastName` | string | Display name (IdentityUser only has UserName) |
| `AccountStatus` | enum | Controls what the user can do (see doc 10) |
| `AccountStatusReason` | string? | Why the status was changed (audit trail) |
| `AccountStatusChangedAtUtc` | DateTime? | When the status was last changed |
| `AccountStatusChangedBy` | string? | Who changed it (CRM manager's user ID) |
| `CreditTier` | CreditTier? | Borrower's credit rating (set during vetting) |
| `CreditLimit` | decimal? | Maximum borrowing amount |
| `CapitalLimit` | decimal? | Maximum lending capital (for Lenders) |
| `BlockedActivity` | string? | What's blocked: "Borrowing", "Lending", or "Both" |
| `TwoFactorSetupComplete` | bool | Whether 2FA has been configured |
| `CreatedAtUtc` | DateTime | Account creation timestamp |
| `LastLoginAtUtc` | DateTime? | Last successful login |

### What IdentityUser Already Provides

The base `IdentityUser` class gives us:
- `Id` (string, GUID by default)
- `UserName`
- `Email` / `EmailConfirmed`
- `PasswordHash`
- `PhoneNumber` / `PhoneNumberConfirmed`
- `TwoFactorEnabled`
- `LockoutEnd` / `LockoutEnabled` / `AccessFailedCount`
- `SecurityStamp` / `ConcurrencyStamp`

---

## CustomRole Entity

**File:** `src/LoanSuperMarket.Domain/Entities/Identity/CustomRole.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class CustomRole : IdentityRole
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
```

**Why extend IdentityRole?**
- `Description` — human-readable explanation of what the role does
- `IsSystemRole` — prevents deletion of predefined roles (Admin, CrmManager, etc.)
- `CreatedBy` — audit trail for who created the role
- `Permissions` — navigation to the granular permission system (see doc 09)

---

## AuthIdentityDbContext

**File:** `src/LoanSuperMarket.Infrastructure/Identity/AuthIdentityDbContext.cs`

```csharp
using LoanSuperMarket.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Identity;

public sealed class AuthIdentityDbContext
    : IdentityDbContext<ApplicationUser, CustomRole, string>
{
    public AuthIdentityDbContext(DbContextOptions<AuthIdentityDbContext> options)
        : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRefreshToken(modelBuilder);
        ConfigureUserSession(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureRecoveryCode(modelBuilder);
        ConfigureApplicationUser(modelBuilder);
        ConfigureCustomRole(modelBuilder);
    }
}
```

### Entity Configuration (Fluent API)

```csharp
private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ApplicationUser>(entity =>
    {
        entity.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.AccountStatusReason)
            .HasMaxLength(500);

        entity.Property(e => e.AccountStatusChangedBy)
            .HasMaxLength(450);

        entity.Property(e => e.BlockedActivity)
            .HasMaxLength(50);

        entity.Property(e => e.CreditLimit)
            .HasPrecision(18, 2);

        entity.Property(e => e.CapitalLimit)
            .HasPrecision(18, 2);
    });
}

private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<RefreshToken>(entity =>
    {
        entity.ToTable("RefreshTokens");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(512);

        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => e.UserId);

        entity.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

---

## UserManager and RoleManager Usage Patterns

### UserManager<ApplicationUser>

`UserManager` is the primary service for user CRUD operations. It's injected into services that need to create, find, or modify users.

```csharp
// Finding users
var user = await _userManager.FindByEmailAsync(email);
var user = await _userManager.FindByIdAsync(userId);

// Creating users (hashes password automatically)
var result = await _userManager.CreateAsync(user, password);
if (!result.Succeeded)
{
    var errors = result.Errors.Select(e => e.Description);
}

// Role management
await _userManager.AddToRoleAsync(user, "Borrower");
await _userManager.RemoveFromRoleAsync(user, "Borrower");
var roles = await _userManager.GetRolesAsync(user);
var usersInRole = await _userManager.GetUsersInRoleAsync("Admin");

// Email confirmation
var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
var result = await _userManager.ConfirmEmailAsync(user, token);

// Password reset
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

// Password change (requires current password)
var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

// Lockout
var isLockedOut = await _userManager.IsLockedOutAsync(user);
var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

// Updating user properties
user.FirstName = "NewName";
var result = await _userManager.UpdateAsync(user);
```

### RoleManager<CustomRole>

```csharp
// Check if role exists
var exists = await _roleManager.RoleExistsAsync("Admin");

// Create a new role
var role = new CustomRole
{
    Name = "NewRole",
    Description = "Description of the role",
    IsSystemRole = false,
    CreatedBy = currentUserId
};
var result = await _roleManager.CreateAsync(role);

// Find a role
var role = await _roleManager.FindByNameAsync("Admin");
```

---

## SignInManager for Password Verification

The project uses `SignInManager` specifically for password verification with lockout tracking:

```csharp
public async Task<bool> ValidateCredentialsAsync(
    string email, string password, CancellationToken cancellationToken = default)
{
    var user = await _userManager.FindByEmailAsync(email);
    if (user is null) return false;

    // CheckPasswordSignInAsync handles lockout tracking automatically
    var result = await _signInManager.CheckPasswordSignInAsync(
        user, password, lockoutOnFailure: true);

    return result.Succeeded;
}
```

**Why `CheckPasswordSignInAsync` instead of `_userManager.CheckPasswordAsync`?**
- `CheckPasswordSignInAsync` automatically increments `AccessFailedCount` on failure
- When `AccessFailedCount` reaches `MaxFailedAccessAttempts` (5), it locks the account
- On success, it resets `AccessFailedCount` to 0
- The `lockoutOnFailure: true` parameter enables this behavior

**Important:** We do NOT use `SignInManager.PasswordSignInAsync` because that creates a cookie-based sign-in. We only validate the password, then issue JWT tokens separately.

---

## How Identity Integrates with the Two-DbContext Architecture

### Registration Flow (Crossing Both Contexts)

```
1. RegisterCommand arrives via MediatR
2. IdentityService.RegisterUserAsync() → uses UserManager (AuthIdentityDbContext)
   - Creates ApplicationUser in AspNetUsers table
   - Hashes password
   - Assigns role via AspNetUserRoles
3. If user is a Borrower → BorrowerRepository creates Borrower entity (ApplicationDbContext)
4. If user is a Lender → LenderRepository creates Lender entity (ApplicationDbContext)
```

### Login Flow

```
1. LoginCommand arrives
2. IdentityService.ValidateCredentialsAsync() → SignInManager (AuthIdentityDbContext)
3. JwtTokenService.GenerateTokensAsync() → creates RefreshToken (AuthIdentityDbContext)
4. SessionService.CreateSessionAsync() → creates UserSession (AuthIdentityDbContext)
```

### Domain Operations

```
1. CreateLoanApplicationCommand arrives
2. AccountStatusBehaviour checks user.AccountStatus (AuthIdentityDbContext via IIdentityService)
3. LimitEnforcementBehaviour checks user.CreditLimit (AuthIdentityDbContext)
4. Handler creates LoanApplication (ApplicationDbContext via ILoanApplicationRepository)
```

### DbContext Registration

Both contexts use the same connection string but are registered independently:

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddDbContext<AuthIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));
```

---

## IdentityService Implementation

**File:** `src/LoanSuperMarket.Infrastructure/Identity/IdentityService.cs`

```csharp
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)>
        RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            AccountStatus = AccountStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return (false, string.Empty, result.Errors.Select(e => e.Description));

        if (!string.IsNullOrWhiteSpace(request.UserType))
            await _userManager.AddToRoleAsync(user, request.UserType);

        return (true, user.Id, Enumerable.Empty<string>());
    }
}
```

---

## Common Pitfalls

| Issue | Cause | Solution |
|-------|-------|----------|
| `[Authorize(Roles=...)]` doesn't work | Used `AddIdentity` instead of `AddIdentityCore` | Switch to `AddIdentityCore` + manual JWT config |
| `SignInManager` not registered | `AddIdentityCore` doesn't include it | Add `.AddSignInManager()` |
| `RoleManager` not available | Roles not registered | Add `.AddRoles<CustomRole>()` |
| Password reset tokens fail | Token providers not registered | Add `.AddDefaultTokenProviders()` |
| Duplicate email allowed | `RequireUniqueEmail` not set | Set `options.User.RequireUniqueEmail = true` |
| User not locked out after failures | `lockoutOnFailure` is false | Use `CheckPasswordSignInAsync(user, pwd, lockoutOnFailure: true)` |


---

## Common Patterns and Recipes

### Pattern 1: Creating a User Programmatically

```csharp
// Use this pattern when you need to create users outside of registration
// (e.g., admin creating a user, bulk import, test seeding)
public async Task<string> CreateUserAsync(
    string email, string password, string firstName, string lastName,
    string role, AccountStatus initialStatus = AccountStatus.Active)
{
    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        EmailConfirmed = true,  // Skip email confirmation for admin-created users
        AccountStatus = initialStatus,
        CreatedAtUtc = DateTime.UtcNow
    };

    var result = await _userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to create user: {errors}");
    }

    await _userManager.AddToRoleAsync(user, role);
    return user.Id;
}
```

### Pattern 2: Changing Account Status Safely

```csharp
// Always record who changed the status and why
public async Task<bool> ChangeAccountStatusAsync(
    string userId, AccountStatus newStatus, string reason, string changedBy)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user is null) return false;

    user.AccountStatus = newStatus;
    user.AccountStatusReason = reason;
    user.AccountStatusChangedAtUtc = DateTime.UtcNow;
    user.AccountStatusChangedBy = changedBy;

    var result = await _userManager.UpdateAsync(user);
    return result.Succeeded;
}
```

### Pattern 3: Querying Users by Status

```csharp
// Using the AuthIdentityDbContext directly for complex queries
public async Task<List<ApplicationUser>> GetUsersByStatusAsync(
    AccountStatus status, CancellationToken ct)
{
    return await _dbContext.Users
        .Where(u => u.AccountStatus == status)
        .OrderBy(u => u.CreatedAtUtc)
        .ToListAsync(ct);
}
```

### Pattern 4: Checking Multiple Roles

```csharp
// Check if a user has ANY of the specified roles
public async Task<bool> HasAnyRoleAsync(string userId, params string[] roles)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user is null) return false;

    var userRoles = await _userManager.GetRolesAsync(user);
    return userRoles.Any(r => roles.Contains(r));
}
```

### Pattern 5: Password Validation Without Login

```csharp
// Validate a password meets policy requirements without creating a user
public async Task<IEnumerable<string>> ValidatePasswordAsync(string password)
{
    var validators = _userManager.PasswordValidators;
    var errors = new List<string>();

    // Create a dummy user for validation
    var dummyUser = new ApplicationUser();

    foreach (var validator in validators)
    {
        var result = await validator.ValidateAsync(_userManager, dummyUser, password);
        if (!result.Succeeded)
        {
            errors.AddRange(result.Errors.Select(e => e.Description));
        }
    }

    return errors;
}
```

---

## Troubleshooting Common Issues

### Issue: "No service for type 'SignInManager<ApplicationUser>' has been registered"

**Cause:** Forgot to add `.AddSignInManager()` after `AddIdentityCore`.

**Fix:**
```csharp
services.AddIdentityCore<ApplicationUser>(options => { ... })
    .AddRoles<CustomRole>()
    .AddEntityFrameworkStores<AuthIdentityDbContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager();  // ← Don't forget this!
```

### Issue: "User.IsInRole() always returns false"

**Cause:** The `ClaimsIdentity` doesn't know which claim type represents roles.

**Fix:** Ensure `RoleClaimType = "role"` in `TokenValidationParameters` AND in the
`OnTokenValidated` event (see Doc 07).

### Issue: "RoleManager not available in DI"

**Cause:** Forgot `.AddRoles<CustomRole>()`.

**Fix:**
```csharp
services.AddIdentityCore<ApplicationUser>(options => { ... })
    .AddRoles<CustomRole>()  // ← This registers RoleManager<CustomRole>
    // ...
```

### Issue: "Cookie authentication overriding JWT"

**Cause:** Used `AddIdentity` instead of `AddIdentityCore`.

**Fix:** Switch to `AddIdentityCore` and manually add the components you need.

### Issue: "Duplicate key violation on AspNetUsers"

**Cause:** Both DbContexts trying to create the same tables.

**Fix:** Only `AuthIdentityDbContext` should manage Identity tables. Ensure migrations
are generated from the correct context:
```bash
dotnet ef migrations add InitialIdentity --context AuthIdentityDbContext
dotnet ef migrations add InitialDomain --context ApplicationDbContext
```

---

## Testing Identity

### Unit Testing with Mocked IIdentityService

```csharp
// In your test project
var mockIdentityService = new Mock<IIdentityService>();

mockIdentityService
    .Setup(x => x.ValidateCredentialsAsync("test@example.com", "password", It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

mockIdentityService
    .Setup(x => x.GetUserByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
    .ReturnsAsync(new ApplicationUser
    {
        Id = "user-123",
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User",
        AccountStatus = AccountStatus.Active
    });

// Use in handler tests
var handler = new LoginCommandHandler(
    mockIdentityService.Object,
    mockTokenService.Object,
    // ... other mocks
);
```

### Integration Testing with Real Identity

```csharp
// Use WebApplicationFactory with in-memory database
public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with in-memory for tests
                services.AddDbContext<AuthIdentityDbContext>(options =>
                    options.UseInMemoryDatabase("TestAuth"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        var request = new
        {
            Email = "newuser@test.com",
            Password = "Test@12345!",
            FirstName = "New",
            LastName = "User",
            UserType = "Borrower"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
    }
}
```
