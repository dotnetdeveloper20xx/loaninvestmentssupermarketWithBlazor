using System.Security.Claims;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Provides access to the current authenticated user's identity and claims
/// by reading from the HTTP context's ClaimsPrincipal.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub");

    /// <inheritdoc />
    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        (User?.FindAll("role")
            .Concat(User?.FindAll(ClaimTypes.Role) ?? [])
            .Select(c => c.Value)
            .Distinct()
            .ToList()
            .AsReadOnly()
        ?? (IReadOnlyList<string>)Array.Empty<string>());

    /// <inheritdoc />
    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public bool IsInRole(string role) =>
        User?.IsInRole(role) ?? false;

    /// <inheritdoc />
    public bool HasPermission(PermissionModule module, PermissionAction action)
    {
        var permissionClaim = $"{module}.{action}";

        return User?.FindAll("permissions")
            .Any(c => c.Value.Equals(permissionClaim, StringComparison.OrdinalIgnoreCase))
            ?? false;
    }
}
