using LoanSuperMarket.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LoanSuperMarket.Infrastructure.Services;

/// <summary>
/// Provides client connection information by reading from the current HTTP context.
/// </summary>
public sealed class ClientInfoProvider : IClientInfoProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientInfoProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
