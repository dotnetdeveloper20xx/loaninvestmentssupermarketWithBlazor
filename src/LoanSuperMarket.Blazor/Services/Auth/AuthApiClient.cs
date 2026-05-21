using System.Net.Http.Json;
using LoanSuperMarket.Shared.Auth;
using LoanSuperMarket.Shared.Common;

namespace LoanSuperMarket.Blazor.Services.Auth;

/// <summary>
/// HTTP client for authentication API endpoints. Handles login, registration,
/// token refresh, logout, and password management operations.
/// On successful login, stores tokens via JwtAuthenticationStateProvider.
/// </summary>
public sealed class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthApiClient(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Authenticates the user with email and password.
    /// On success, stores tokens and notifies the auth state provider.
    /// </summary>
    public async Task<ApiResponse<AuthTokenResponse>?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request,
            cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(
            cancellationToken);

        if (result?.Success == true && result.Data is not null)
        {
            await _authStateProvider.MarkUserAsAuthenticatedAsync(result.Data);
        }

        return result;
    }

    /// <summary>
    /// Refreshes the access token using a valid refresh token.
    /// </summary>
    public async Task<ApiResponse<AuthTokenResponse>?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var request = new { RefreshToken = refreshToken };

        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/refresh-token",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(
            cancellationToken);
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    public async Task<ApiResponse<string>?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }

    /// <summary>
    /// Logs out the current user by revoking the refresh token on the server
    /// and clearing local auth state.
    /// </summary>
    public async Task<ApiResponse<string>?> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _authStateProvider.GetRefreshTokenAsync();

        // Send the refresh token to the server for revocation
        var request = new { RefreshToken = refreshToken ?? string.Empty };

        ApiResponse<string>? result = null;

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/logout",
                request,
                cancellationToken);

            result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
                cancellationToken);
        }
        catch
        {
            // Even if the server call fails, we still want to clear local state
        }

        await _authStateProvider.MarkUserAsLoggedOutAsync();

        return result;
    }

    /// <summary>
    /// Initiates the forgot password flow by sending a reset link to the email.
    /// </summary>
    public async Task<ApiResponse<string>?> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var request = new ForgotPasswordRequest { Email = email };

        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/forgot-password",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    public async Task<ApiResponse<string>?> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/reset-password",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }
}
