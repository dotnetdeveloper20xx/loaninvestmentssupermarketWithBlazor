namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Provides client connection information (e.g., IP address) for the current request.
/// </summary>
public interface IClientInfoProvider
{
    /// <summary>
    /// Gets the IP address of the current client, or null if unavailable.
    /// </summary>
    string? IpAddress { get; }
}
