namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Information about the client device/browser for session tracking.
/// </summary>
public sealed record SessionInfo(
    string? DeviceType,
    string? IpAddress,
    string? Browser);
