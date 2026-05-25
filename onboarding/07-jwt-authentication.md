# 07 — JWT Authentication

## Overview

The Loan Investment Supermarket uses **JWT (JSON Web Tokens)** for stateless authentication between the Blazor WebAssembly frontend and the ASP.NET Core API. The system implements:

- Short-lived access tokens (15 minutes)
- Long-lived refresh tokens (7 or 30 days)
- Token rotation with reuse detection
- Role and permission claims embedded in the JWT
- Automatic token refresh on the client side

---

## Architecture Flow

```
┌──────────────────┐         ┌──────────────────┐         ┌──────────────┐
│  Blazor WASM     │         │  ASP.NET Core    │         │  SQL Server  │
│  (Client)        │         │  API             │         │              │
├──────────────────┤         ├──────────────────┤         ├──────────────┤
│                  │ POST    │                  │         │              │
│  Login Form  ────┼────────►│  AuthController  │         │              │
│                  │         │       │          │         │              │
│                  │         │       ▼          │         │              │
│                  │         │  JwtTokenService │────────►│ RefreshTokens│
│                  │         │       │          │         │              │
│                  │◄────────┼───────┘          │         │              │
│  Store tokens    │ 200 OK  │  {accessToken,   │         │              │
│  in memory       │         │   refreshToken,  │         │              │
│                  │         │   expiresAt}     │         │              │
├──────────────────┤         ├──────────────────┤         ├──────────────┤
│                  │ GET     │                  │         │              │
│  API Request ────┼────────►│  JWT Middleware  │         │              │
│  Bearer: token   │         │  validates token │         │              │
│                  │◄────────┼──────────────────┤         │              │
│                  │ 200/401 │                  │         │              │
└──────────────────┘         └──────────────────┘         └──────────────┘
```

---

## JwtSettings Configuration

### Configuration Class

**File:** `src/LoanSuperMarket.Shared/Configuration/JwtSettings.cs`

```csharp
namespace LoanSuperMarket.Shared.Configuration;

/// <summary>
/// Configuration model for JWT token generation and validation settings.
/// Bound from the "JwtSettings" section in appsettings.json.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// HMAC-SHA256 secret key for signing tokens. Must be at least 256 bits (32 bytes).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    public int RememberMeRefreshTokenExpirationDays { get; set; } = 30;
}
```

### appsettings.json

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast256BitsLong!@#$%^&*()_+",
    "Issuer": "LoanSuperMarket.Api",
    "Audience": "LoanSuperMarket.Blazor",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "RememberMeRefreshTokenExpirationDays": 30
  }
}
```

| Setting | Purpose | Default |
|---------|---------|---------|
| `SecretKey` | HMAC-SHA256 signing key (≥32 bytes) | — |
| `Issuer` | Token issuer claim (`iss`) | `LoanSuperMarket.Api` |
| `Audience` | Token audience claim (`aud`) | `LoanSuperMarket.Blazor` |
| `AccessTokenExpirationMinutes` | How long access tokens are valid | 15 min |
| `RefreshTokenExpirationDays` | Normal refresh token lifetime | 7 days |
| `RememberMeRefreshTokenExpirationDays` | Extended lifetime when "Remember Me" is checked | 30 days |

### Binding in Program.cs

```csharp
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
```

---

## JwtTokenService — Generating Access Tokens

**File:** `src/LoanSuperMarket.Infrastructure/Identity/JwtTokenService.cs`

### Interface

```csharp
public interface ITokenService
{
    Task<AuthTokenResponse> GenerateTokensAsync(
        ApplicationUser user, bool rememberMe = false,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string refreshToken, string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllUserTokensAsync(
        string userId, string reason,
        CancellationToken cancellationToken = default);
}
```

### Constructor and Dependencies

```csharp
public sealed class JwtTokenService : ITokenService
{
    private const int MinimumKeyLengthBytes = 32; // 256 bits

    private readonly AuthIdentityDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(
        AuthIdentityDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;

        ValidateSecretKey(_jwtSettings.SecretKey);

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        _signingCredentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);
    }
```

**Key point:** The secret key is validated at startup. If it's less than 256 bits, the service throws immediately — fail fast rather than generating insecure tokens.

---

## How Claims Are Structured in the JWT Payload

### GenerateAccessToken Method

```csharp
private string GenerateAccessToken(
    ApplicationUser user,
    IList<string> roles,
    IReadOnlyList<string> permissions)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new(JwtRegisteredClaimNames.GivenName, user.FirstName),
        new(JwtRegisteredClaimNames.FamilyName, user.LastName),
        new("account_status", user.AccountStatus.ToString()),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(JwtRegisteredClaimNames.Iat,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ClaimValueTypes.Integer64)
    };

    // Add role claims
    foreach (var role in roles)
    {
        claims.Add(new Claim("role", role));
    }

    // Add permission claims (format: "Module.Action")
    foreach (var permission in permissions)
    {
        claims.Add(new Claim("permissions", permission));
    }

    var now = DateTime.UtcNow;
    var expires = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims, "jwt", "email", "role"),
        Expires = expires,
        NotBefore = now,
        IssuedAt = now,
        Issuer = _jwtSettings.Issuer,
        Audience = _jwtSettings.Audience,
        SigningCredentials = _signingCredentials
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    tokenHandler.OutboundClaimTypeMap.Clear();
    var token = tokenHandler.CreateToken(tokenDescriptor);

    return tokenHandler.WriteToken(token);
}
```

### Decoded JWT Payload Example

```json
{
  "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "email": "john@example.com",
  "given_name": "John",
  "family_name": "Doe",
  "account_status": "Active",
  "jti": "unique-token-id",
  "iat": 1700000000,
  "role": "Borrower",
  "permissions": [
    "LoanManagement.View",
    "LoanManagement.Create"
  ],
  "exp": 1700000900,
  "nbf": 1700000000,
  "iss": "LoanSuperMarket.Api",
  "aud": "LoanSuperMarket.Blazor"
}
```

### Critical Detail: `OutboundClaimTypeMap.Clear()`

```csharp
tokenHandler.OutboundClaimTypeMap.Clear();
```

Without this line, the `JwtSecurityTokenHandler` would map short claim names to long XML URIs:
- `"role"` → `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"`
- `"email"` → `"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"`

We clear the map so claims stay as short names (`"role"`, `"email"`) in the token payload.

---

## Refresh Token Generation, Storage, Rotation, and Revocation

### The RefreshToken Entity

**File:** `src/LoanSuperMarket.Domain/Entities/Identity/RefreshToken.cs`

```csharp
public sealed class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsRememberMe { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
```

### Creating a Refresh Token

```csharp
private async Task<RefreshToken> CreateRefreshTokenAsync(
    string userId, bool rememberMe, CancellationToken cancellationToken)
{
    var expirationDays = rememberMe
        ? _jwtSettings.RememberMeRefreshTokenExpirationDays
        : _jwtSettings.RefreshTokenExpirationDays;

    var refreshToken = new RefreshToken
    {
        Token = GenerateSecureRandomToken(),
        UserId = userId,
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(expirationDays),
        IsRememberMe = rememberMe
    };

    _dbContext.RefreshTokens.Add(refreshToken);
    await _dbContext.SaveChangesAsync(cancellationToken);

    return refreshToken;
}

private static string GenerateSecureRandomToken()
{
    var randomBytes = RandomNumberGenerator.GetBytes(64);
    return Convert.ToBase64String(randomBytes);
}
```

**Key points:**
- Refresh tokens are **opaque** (random bytes, not JWTs)
- 64 bytes of cryptographic randomness = 512 bits of entropy
- Stored in the database for validation and revocation
- `IsRememberMe` determines the expiration (7 vs 30 days)

### Token Rotation (Refresh Flow)

```csharp
public async Task<AuthTokenResponse> RefreshTokenAsync(
    string refreshToken, CancellationToken cancellationToken = default)
{
    var storedToken = await _dbContext.RefreshTokens
        .Include(rt => rt.User)
        .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

    if (storedToken is null)
        throw new UnauthorizedAccessException("Invalid refresh token.");

    // REUSE DETECTION: if a revoked token is presented, revoke ALL tokens
    if (storedToken.IsRevoked)
    {
        await RevokeAllUserTokensAsync(
            storedToken.UserId,
            "Attempted reuse of revoked refresh token (potential token theft)",
            cancellationToken);

        throw new UnauthorizedAccessException(
            "Refresh token has been revoked. All sessions terminated for security.");
    }

    if (storedToken.IsExpired)
        throw new UnauthorizedAccessException("Refresh token has expired.");

    // ROTATE: revoke current token, issue new one
    storedToken.RevokedAtUtc = DateTime.UtcNow;
    storedToken.RevokedReason = "Rotated during refresh";

    var newRefreshToken = await CreateRefreshTokenAsync(
        storedToken.User.Id, storedToken.IsRememberMe, cancellationToken);

    // Link old → new for audit trail
    storedToken.ReplacedByToken = newRefreshToken.Token;

    await _dbContext.SaveChangesAsync(cancellationToken);

    // Generate new access token with CURRENT roles (reflects any role changes)
    var roles = await _userManager.GetRolesAsync(storedToken.User);
    var permissions = await GetUserPermissionsAsync(
        storedToken.User.Id, roles, cancellationToken);
    var accessToken = GenerateAccessToken(storedToken.User, roles, permissions);

    return new AuthTokenResponse
    {
        AccessToken = accessToken,
        RefreshToken = newRefreshToken.Token,
        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
    };
}
```

### Reuse Detection — Why It Matters

```
Normal flow:
  Token A → (rotate) → Token B → (rotate) → Token C

Attack scenario:
  Attacker steals Token A
  Legitimate user uses Token A → gets Token B (Token A revoked)
  Attacker tries Token A → REVOKED! → ALL tokens revoked → user must re-login
```

This is a critical security feature. If a revoked token is ever presented, it means either:
1. A replay attack (token was stolen)
2. A client bug (using an old token)

Either way, the safest response is to revoke everything and force re-authentication.

---

## Token Validation in Program.cs

**File:** `src/LoanSuperMarket.Api/Program.cs`

```csharp
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,

        NameClaimType = "email",
        RoleClaimType = "role"
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("X-Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                if (identity.RoleClaimType != "role")
                {
                    var newIdentity = new ClaimsIdentity(
                        identity.Claims, identity.AuthenticationType, "email", "role");
                    context.Principal = new ClaimsPrincipal(newIdentity);
                }
            }
            return Task.CompletedTask;
        }
    };
});
```

### Critical Settings Explained

| Setting | Value | Why |
|---------|-------|-----|
| `MapInboundClaims = false` | Prevents .NET from remapping claim types | Without this, `"role"` becomes the long XML URI and `[Authorize(Roles=...)]` breaks |
| `ClockSkew = 30 seconds` | Tight tolerance for token expiration | Default is 5 minutes which is too generous; 30s handles minor clock drift |
| `NameClaimType = "email"` | `User.Identity.Name` returns the email | Matches the `"email"` claim in our token |
| `RoleClaimType = "role"` | `User.IsInRole()` checks the `"role"` claim | Matches our custom role claim name |
| `X-Token-Expired` header | Signals the client that the token expired | Client can distinguish "expired" from "invalid" and attempt refresh |

### The AddIdentityCore vs AddIdentity Issue

This project uses `AddIdentityCore` instead of `AddIdentity`:

```csharp
// ✅ What we use:
services.AddIdentityCore<ApplicationUser>(options => { ... })
    .AddRoles<CustomRole>()
    .AddEntityFrameworkStores<AuthIdentityDbContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager();

// ❌ What we DON'T use:
services.AddIdentity<ApplicationUser, CustomRole>(options => { ... })
    .AddEntityFrameworkStores<AuthIdentityDbContext>();
```

**Why this matters:**
- `AddIdentity` registers cookie authentication as the default scheme, which conflicts with JWT Bearer
- `AddIdentityCore` only registers UserManager, doesn't touch authentication schemes
- We manually add `.AddSignInManager()` because `AddIdentityCore` doesn't include it by default
- We manually add `.AddRoles<CustomRole>()` to enable RoleManager

If you accidentally use `AddIdentity`, the JWT Bearer scheme won't be the default, and all `[Authorize]` attributes will try to redirect to a login page instead of returning 401.

---

## Token Expiration and Clock Skew

### Access Token Lifetime

- **Duration:** 15 minutes (configurable via `AccessTokenExpirationMinutes`)
- **Clock Skew:** 30 seconds (allows for minor time differences between servers)
- **Effective window:** A token issued at T=0 is valid until T=15:30

### Why 15 Minutes?

Short-lived access tokens limit the damage window if a token is stolen:
- Attacker has at most 15 minutes of access
- No need to check a revocation list on every request (stateless)
- Role/permission changes take effect on next token refresh

### Refresh Token Lifetime

| Scenario | Duration |
|----------|----------|
| Normal login | 7 days |
| "Remember Me" checked | 30 days |

### Token Expiration Header

When a token expires, the API adds a custom header:

```csharp
OnAuthenticationFailed = context =>
{
    if (context.Exception is SecurityTokenExpiredException)
    {
        context.Response.Headers.Append("X-Token-Expired", "true");
    }
    return Task.CompletedTask;
};
```

The Blazor client can check for this header to distinguish "token expired" (refresh needed) from "token invalid" (re-login needed).

---

## How the Blazor Client Stores and Sends Tokens

### AuthTokenHandler (DelegatingHandler)

**File:** `src/LoanSuperMarket.Blazor/Services/Auth/AuthTokenHandler.cs`

```csharp
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

    public AuthTokenHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authStateProvider = _serviceProvider
            .GetRequiredService<JwtAuthenticationStateProvider>();

        // 1. Attach Bearer token to every outgoing request
        var accessToken = await authStateProvider.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // 2. Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // 3. If 401 and not an auth endpoint, try to refresh
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

### How It Works

1. **Every HTTP request** goes through `AuthTokenHandler`
2. The handler reads the access token from `JwtAuthenticationStateProvider` (stored in browser memory/localStorage)
3. Attaches it as `Authorization: Bearer <token>`
4. If the API returns **401 Unauthorized**:
   - Checks if this is an auth endpoint (to avoid infinite loops)
   - Attempts to refresh using the stored refresh token
   - If refresh succeeds: retries the original request with the new token
   - If refresh fails: clears auth state and redirects to `/login`

### Registration in Blazor Program.cs

```csharp
// Register AuthTokenHandler
builder.Services.AddScoped<AuthTokenHandler>();

// Register HttpClient with the handler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});
```

### Token Storage Strategy

Tokens are stored via `JwtAuthenticationStateProvider`:
- **Access Token:** In-memory (lost on page refresh, re-obtained via refresh token)
- **Refresh Token:** Browser localStorage (persists across page refreshes)
- On app startup, if a refresh token exists in localStorage, the client automatically refreshes to get a new access token

---

## Complete Token Lifecycle

```
1. User logs in → API returns {accessToken, refreshToken, expiresAt}
2. Client stores tokens via JwtAuthenticationStateProvider
3. Every API call → AuthTokenHandler attaches Bearer token
4. After 15 min → token expires → API returns 401
5. AuthTokenHandler catches 401 → calls /api/auth/refresh-token
6. API rotates refresh token → returns new {accessToken, refreshToken}
7. Client stores new tokens → retries original request
8. If refresh fails → client redirects to /login
```

---

## DI Registration

```csharp
// In Program.cs (API)
builder.Services.AddScoped<ITokenService, JwtTokenService>();
```

Note: This is registered in `Program.cs` directly, not in the Infrastructure `DependencyInjection.cs`, because it depends on `JwtSettings` which is configured in the API project.

---

## Security Considerations

| Concern | Mitigation |
|---------|-----------|
| Token theft | Short-lived (15 min), refresh rotation with reuse detection |
| Weak signing key | Validated at startup (≥256 bits required) |
| Clock skew attacks | Tight 30-second tolerance |
| XSS token extraction | Access token in memory, not localStorage |
| Refresh token replay | Reuse detection revokes ALL user tokens |
| Stale permissions | Permissions refreshed on every token rotation |
| Infinite refresh loops | Auth endpoints excluded from 401 interception |


---

## Troubleshooting JWT Issues

### Issue: "IDX10214: Audience validation failed"

**Cause:** The `aud` claim in the token doesn't match `ValidAudience` in validation params.

**Fix:** Ensure `JwtSettings.Audience` is the same value used in both token generation
and validation:
```json
{
  "JwtSettings": {
    "Audience": "LoanSuperMarket.Blazor"  // Must match exactly
  }
}
```

### Issue: "IDX10205: Issuer validation failed"

**Cause:** Same as above but for the `iss` claim.

**Fix:** Ensure `JwtSettings.Issuer` matches in both places.

### Issue: Token is valid but User.Identity.Name is null

**Cause:** `NameClaimType` doesn't match the claim in the token.

**Fix:** We use `email` as the name claim:
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    NameClaimType = "email",  // Must match the claim type in the token
    // ...
};
```

### Issue: Claims are mapped to long URI types

**Cause:** `MapInboundClaims` defaults to `true`, which remaps:
- `sub` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
- `email` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`
- `role` → `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`

**Fix:**
```csharp
options.MapInboundClaims = false;  // Keep claims as-is from the JWT
```

### Issue: Token refresh creates infinite loop

**Cause:** The refresh endpoint itself returns 401, triggering another refresh attempt.

**Fix:** The `AuthTokenHandler` excludes auth endpoints from 401 interception:
```csharp
private static readonly string[] AuthEndpoints =
[
    "api/auth/login",
    "api/auth/register",
    "api/auth/refresh-token",  // ← This prevents the loop
    "api/auth/forgot-password",
    "api/auth/reset-password"
];
```

### Issue: "JWT secret key must be at least 256 bits"

**Cause:** The configured secret key is too short.

**Fix:** Generate a key that's at least 32 characters:
```bash
# Generate a secure key (PowerShell)
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])

# Or use a passphrase that's 32+ characters
"YourSuperSecretKeyThatIsAtLeast256BitsLong!@#$%^&*()_+"
```

---

## Testing JWT Authentication

### Generating Test Tokens

```csharp
public static class TestTokenGenerator
{
    private const string TestSecret = "TestSecretKeyThatIsAtLeast256BitsLongForTesting!@#$";

    public static string GenerateToken(
        string userId = "test-user-id",
        string email = "test@example.com",
        string[] roles = null,
        string[] permissions = null,
        int expiresInMinutes = 15)
    {
        roles ??= ["Borrower"];
        permissions ??= ["LoanManagement.View", "LoanManagement.Create"];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.GivenName, "Test"),
            new(JwtRegisteredClaimNames.FamilyName, "User"),
            new("account_status", "Active"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permissions", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "LoanSuperMarket.Api",
            audience: "LoanSuperMarket.Blazor",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Integration Test: Protected Endpoint

```csharp
[Fact]
public async Task ProtectedEndpoint_WithoutToken_Returns401()
{
    var response = await _client.GetAsync("/api/sessions/my");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task ProtectedEndpoint_WithValidToken_Returns200()
{
    var token = TestTokenGenerator.GenerateToken(roles: ["Borrower"]);
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var response = await _client.GetAsync("/api/sessions/my");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
{
    var token = TestTokenGenerator.GenerateToken(expiresInMinutes: -1);
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var response = await _client.GetAsync("/api/sessions/my");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("true", response.Headers.GetValues("X-Token-Expired").First());
}
```

### Unit Test: Token Generation

```csharp
[Fact]
public async Task GenerateTokensAsync_IncludesCorrectClaims()
{
    // Arrange
    var user = new ApplicationUser
    {
        Id = "user-123",
        Email = "test@example.com",
        FirstName = "John",
        LastName = "Doe",
        AccountStatus = AccountStatus.Active
    };

    _mockUserManager.Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(new List<string> { "Borrower" });

    // Act
    var result = await _tokenService.GenerateTokensAsync(user);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var token = handler.ReadJwtToken(result.AccessToken);

    Assert.Equal("user-123", token.Claims.First(c => c.Type == "sub").Value);
    Assert.Equal("test@example.com", token.Claims.First(c => c.Type == "email").Value);
    Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "Borrower");
    Assert.True(result.ExpiresAt > DateTime.UtcNow);
    Assert.NotEmpty(result.RefreshToken);
}
```

### Unit Test: Refresh Token Rotation

```csharp
[Fact]
public async Task RefreshTokenAsync_RotatesToken()
{
    // Arrange: create initial tokens
    var user = CreateTestUser();
    var initial = await _tokenService.GenerateTokensAsync(user);

    // Act: refresh
    var refreshed = await _tokenService.RefreshTokenAsync(initial.RefreshToken);

    // Assert
    Assert.NotEqual(initial.AccessToken, refreshed.AccessToken);
    Assert.NotEqual(initial.RefreshToken, refreshed.RefreshToken);

    // Old token should be revoked
    var oldToken = await _dbContext.RefreshTokens
        .FirstAsync(t => t.Token == initial.RefreshToken);
    Assert.NotNull(oldToken.RevokedAtUtc);
    Assert.Equal("Rotated during refresh", oldToken.RevokedReason);
}

[Fact]
public async Task RefreshTokenAsync_DetectsReuse_RevokesAllTokens()
{
    // Arrange
    var user = CreateTestUser();
    var initial = await _tokenService.GenerateTokensAsync(user);

    // First refresh (legitimate)
    await _tokenService.RefreshTokenAsync(initial.RefreshToken);

    // Act: try to reuse the old token (simulates theft)
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        _tokenService.RefreshTokenAsync(initial.RefreshToken));

    // Assert: all tokens revoked
    Assert.Contains("terminated for security", ex.Message);
    var activeTokens = await _dbContext.RefreshTokens
        .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
        .CountAsync();
    Assert.Equal(0, activeTokens);
}
```
