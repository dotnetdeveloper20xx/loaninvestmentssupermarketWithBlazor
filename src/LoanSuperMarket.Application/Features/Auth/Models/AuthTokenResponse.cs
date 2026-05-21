namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Response model containing authentication tokens issued after successful login or token refresh.
/// </summary>
public sealed class AuthTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}
