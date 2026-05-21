using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoanSuperMarket.Shared.Auth;
using LoanSuperMarket.Shared.Common;
using Microsoft.AspNetCore.Components;

namespace LoanSuperMarket.Blazor.Services.Auth;

/// <summary>
/// DelegatingHandler that attaches the Bearer token to all outgoing HTTP requests
/// and handles 401 responses by attempting a token refresh. If refresh fails,
/// redirects the user to the login page.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private bool _isRefreshing;

    // Auth endpoints that should not trigger 401 interception to avoid infinite loops
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
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authStateProvider = _serviceProvider.GetRequiredService<JwtAuthenticationStateProvider>();

        // Attach the access token to the request if available
        var accessToken = await authStateProvider.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Skip 401 interception for auth endpoints to avoid infinite loops
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
                {
                    return refreshedResponse;
                }

                // Refresh failed - clear auth state and redirect to login
                await authStateProvider.MarkUserAsLoggedOutAsync();

                var navigationManager = _serviceProvider.GetRequiredService<NavigationManager>();
                navigationManager.NavigateTo("/login", forceLoad: true);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }

    /// <summary>
    /// Attempts to refresh the token and retry the original request.
    /// Returns the new response if successful, or null if refresh failed.
    /// </summary>
    private async Task<HttpResponseMessage?> TryRefreshAndRetryAsync(
        HttpRequestMessage originalRequest,
        JwtAuthenticationStateProvider authStateProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await authStateProvider.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            // Use the base handler directly for the refresh call to avoid recursion
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post,
                new Uri(originalRequest.RequestUri!, "api/auth/refresh-token"))
            {
                Content = JsonContent.Create(new { RefreshToken = refreshToken })
            };

            // Build the absolute URI for the refresh endpoint
            if (originalRequest.RequestUri?.GetLeftPart(UriPartial.Authority) is { } baseUrl)
            {
                refreshRequest.RequestUri = new Uri($"{baseUrl}/api/auth/refresh-token");
            }

            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>(
                cancellationToken);

            if (result?.Success != true || result.Data is null)
            {
                return null;
            }

            // Store the new tokens
            await authStateProvider.MarkUserAsAuthenticatedAsync(result.Data);

            // Retry the original request with the new token
            var retryRequest = await CloneHttpRequestMessageAsync(originalRequest);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.Data.AccessToken);

            return await base.SendAsync(retryRequest, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the request is targeting an auth endpoint that should not trigger 401 interception.
    /// </summary>
    private static bool IsAuthEndpoint(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }

        var path = requestUri.AbsolutePath.TrimStart('/');
        return AuthEndpoints.Any(endpoint =>
            path.Equals(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clones an HttpRequestMessage since they cannot be sent twice.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            if (request.Content.Headers.ContentType is not null)
            {
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var property in request.Options)
        {
            clone.Options.TryAdd(property.Key, property.Value);
        }

        return clone;
    }
}
