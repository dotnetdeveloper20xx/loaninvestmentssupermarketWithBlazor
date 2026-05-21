namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries/commands that are allowed for users with PendingApproval account status.
/// Typically profile-viewing queries implement this interface.
/// </summary>
public interface IAllowPendingApproval
{
}
