namespace LoanSuperMarket.Shared.Users;

/// <summary>
/// DTO representing an active user session with device and activity information.
/// </summary>
public sealed class UserSessionDto
{
    public Guid Id { get; set; }

    public string? DeviceType { get; set; }

    public string? IpAddress { get; set; }

    public string? Browser { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastActivityAtUtc { get; set; }

    public bool IsActive { get; set; }
}
