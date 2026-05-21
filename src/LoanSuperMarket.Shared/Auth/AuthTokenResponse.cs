namespace LoanSuperMarket.Shared.Auth;

/// <summary>
/// Response model containing authentication tokens issued after successful login or token refresh.
/// Used by the Blazor WASM client to store and manage tokens.
/// </summary>
public sealed class AuthTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}
