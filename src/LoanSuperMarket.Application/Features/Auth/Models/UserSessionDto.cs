namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// DTO representing a user session for display purposes.
/// </summary>
public sealed record UserSessionDto(
    Guid Id,
    string? DeviceType,
    string? IpAddress,
    string? Browser,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc,
    bool IsActive);
