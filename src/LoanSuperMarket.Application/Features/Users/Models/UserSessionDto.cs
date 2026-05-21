namespace LoanSuperMarket.Application.Features.Users.Models;

/// <summary>
/// DTO representing an active user session with device and activity information.
/// </summary>
public sealed class UserSessionDto
{
    public Guid Id { get; init; }

    public string? DeviceType { get; init; }

    public string? IpAddress { get; init; }

    public string? Browser { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime LastActivityAtUtc { get; init; }

    public bool IsActive { get; init; }
}
