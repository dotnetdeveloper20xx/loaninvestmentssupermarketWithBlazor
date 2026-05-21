using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using LoanSuperMarket.Shared.Auth;

namespace LoanSuperMarket.Blazor.Services.Auth;

/// <summary>
/// Custom AuthenticationStateProvider that reads JWT tokens from localStorage,
/// parses claims to build ClaimsPrincipal, monitors token expiration, and
/// triggers automatic refresh before expiry.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IAsyncDisposable
{
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const int RefreshBufferMinutes = 2;
    private const int ExpiryCheckIntervalSeconds = 30;

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private Timer? _expiryTimer;
    private bool _disposed;

    public JwtAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the current authentication state by reading and validating the JWT from localStorage.
    /// If the token is expired, attempts a refresh before reporting unauthenticated.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await GetTokenFromStorageAsync(AccessTokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return CreateAnonymousState();
        }

        var claims = ParseClaimsFromJwt(token);
        if (claims is null || claims.Length == 0)
        {
            return CreateAnonymousState();
        }

        // Check if token is expired
        if (IsTokenExpired(claims))
        {
            // Attempt refresh before reporting unauthenticated
            var refreshed = await TryRefreshTokenAsync();
            if (!refreshed)
            {
                await RemoveTokensFromStorageAsync();
                return CreateAnonymousState();
            }

            // Re-read the new token after refresh
            token = await GetTokenFromStorageAsync(AccessTokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return CreateAnonymousState();
            }

            claims = ParseClaimsFromJwt(token);
            if (claims is null || claims.Length == 0)
            {
                return CreateAnonymousState();
            }
        }

        StartExpiryMonitor(claims);

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    /// <summary>
    /// Marks the user as authenticated by storing tokens and notifying subscribers.
    /// </summary>
    public async Task MarkUserAsAuthenticatedAsync(AuthTokenResponse tokenResponse)
    {
        await SetTokenInStorageAsync(AccessTokenKey, tokenResponse.AccessToken);
        await SetTokenInStorageAsync(RefreshTokenKey, tokenResponse.RefreshToken);

        var claims = ParseClaimsFromJwt(tokenResponse.AccessToken);
        if (claims is not null && claims.Length > 0)
        {
            StartExpiryMonitor(claims);

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);
            var state = Task.FromResult(new AuthenticationState(principal));
            NotifyAuthenticationStateChanged(state);
        }
    }

    /// <summary>
    /// Marks the user as logged out by removing tokens and notifying subscribers.
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        StopExpiryMonitor();
        await RemoveTokensFromStorageAsync();

        var state = Task.FromResult(CreateAnonymousState());
        NotifyAuthenticationStateChanged(state);
    }

    /// <summary>
    /// Gets the current access token from localStorage.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        return await GetTokenFromStorageAsync(AccessTokenKey);
    }

    /// <summary>
    /// Gets the current refresh token from localStorage.
    /// </summary>
    public async Task<string?> GetRefreshTokenAsync()
    {
        return await GetTokenFromStorageAsync(RefreshTokenKey);
    }

    #region Token Parsing

    /// <summary>
    /// Parses claims from a JWT token by decoding the payload (second segment).
    /// </summary>
    private static Claim[]? ParseClaimsFromJwt(string jwt)
    {
        var segments = jwt.Split('.');
        if (segments.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = segments[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

            if (keyValuePairs is null)
            {
                return null;
            }

            var claims = new List<Claim>();

            foreach (var kvp in keyValuePairs)
            {
                var claimType = MapJwtClaimType(kvp.Key);

                if (kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    // Handle array claims (e.g., roles, permissions)
                    foreach (var element in kvp.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(claimType, element.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(claimType, kvp.Value.ToString()));
                }
            }

            return claims.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Maps JWT registered claim names to .NET ClaimTypes where applicable.
    /// </summary>
    private static string MapJwtClaimType(string jwtClaimType)
    {
        return jwtClaimType switch
        {
            "sub" => ClaimTypes.NameIdentifier,
            "email" => ClaimTypes.Email,
            "given_name" => ClaimTypes.GivenName,
            "family_name" => ClaimTypes.Surname,
            "roles" => ClaimTypes.Role,
            "role" => ClaimTypes.Role,
            _ => jwtClaimType
        };
    }

    /// <summary>
    /// Decodes a Base64Url-encoded string (without padding) to bytes.
    /// </summary>
    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // Replace URL-safe characters
        base64 = base64.Replace('-', '+').Replace('_', '/');

        // Add padding if necessary
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    #endregion

    #region Token Expiration

    /// <summary>
    /// Checks if the token is expired based on the 'exp' claim.
    /// </summary>
    private static bool IsTokenExpired(Claim[] claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim is null || !long.TryParse(expClaim.Value, out var expUnix))
        {
            return true; // No exp claim means we treat it as expired
        }

        var expiration = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        return DateTime.UtcNow >= expiration;
    }

    /// <summary>
    /// Gets the expiration time from claims.
    /// </summary>
    private static DateTime? GetTokenExpiration(Claim[] claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim is null || !long.TryParse(expClaim.Value, out var expUnix))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
    }

    #endregion

    #region Expiry Monitor

    /// <summary>
    /// Starts a timer that checks token expiration every 30 seconds and triggers
    /// a refresh 2 minutes before the token expires.
    /// </summary>
    private void StartExpiryMonitor(Claim[] claims)
    {
        StopExpiryMonitor();

        var expiration = GetTokenExpiration(claims);
        if (expiration is null)
        {
            return;
        }

        _expiryTimer = new Timer(
            async _ => await CheckAndRefreshTokenAsync(),
            null,
            TimeSpan.FromSeconds(ExpiryCheckIntervalSeconds),
            TimeSpan.FromSeconds(ExpiryCheckIntervalSeconds));
    }

    /// <summary>
    /// Stops the expiry monitoring timer.
    /// </summary>
    private void StopExpiryMonitor()
    {
        _expiryTimer?.Dispose();
        _expiryTimer = null;
    }

    /// <summary>
    /// Timer callback that checks if the token is about to expire and triggers refresh.
    /// </summary>
    private async Task CheckAndRefreshTokenAsync()
    {
        try
        {
            var token = await GetTokenFromStorageAsync(AccessTokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                StopExpiryMonitor();
                return;
            }

            var claims = ParseClaimsFromJwt(token);
            if (claims is null)
            {
                StopExpiryMonitor();
                return;
            }

            var expiration = GetTokenExpiration(claims);
            if (expiration is null)
            {
                StopExpiryMonitor();
                return;
            }

            var timeUntilExpiry = expiration.Value - DateTime.UtcNow;

            // Trigger refresh 2 minutes before expiry
            if (timeUntilExpiry <= TimeSpan.FromMinutes(RefreshBufferMinutes))
            {
                var refreshed = await TryRefreshTokenAsync();
                if (!refreshed)
                {
                    // Refresh failed - notify as logged out
                    StopExpiryMonitor();
                    await RemoveTokensFromStorageAsync();
                    var state = Task.FromResult(CreateAnonymousState());
                    NotifyAuthenticationStateChanged(state);
                }
            }
        }
        catch
        {
            // Swallow exceptions in the background timer to avoid crashing
        }
    }

    #endregion

    #region Token Refresh

    /// <summary>
    /// Attempts to refresh the token using the stored refresh token.
    /// Returns true if successful, false otherwise.
    /// </summary>
    private async Task<bool> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await GetTokenFromStorageAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            var request = new { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh-token", request);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ApiRefreshResponse>();
            if (result?.Success != true || result.Data is null)
            {
                return false;
            }

            await SetTokenInStorageAsync(AccessTokenKey, result.Data.AccessToken);
            await SetTokenInStorageAsync(RefreshTokenKey, result.Data.RefreshToken);

            // Restart the expiry monitor with new token claims
            var claims = ParseClaimsFromJwt(result.Data.AccessToken);
            if (claims is not null && claims.Length > 0)
            {
                StartExpiryMonitor(claims);

                // Notify subscribers of updated auth state
                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);
                var state = Task.FromResult(new AuthenticationState(principal));
                NotifyAuthenticationStateChanged(state);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region LocalStorage Access

    private async Task<string?> GetTokenFromStorageAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task SetTokenInStorageAsync(string key, string value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    private async Task RemoveTokensFromStorageAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    #endregion

    #region Helpers

    private static AuthenticationState CreateAnonymousState()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopExpiryMonitor();
            await ValueTask.CompletedTask;
        }
    }

    #endregion

    #region Internal Response Model

    /// <summary>
    /// Internal model for deserializing the refresh token API response.
    /// </summary>
    private sealed class ApiRefreshResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public AuthTokenResponseData? Data { get; init; }
    }

    private sealed class AuthTokenResponseData
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }

    #endregion
}
