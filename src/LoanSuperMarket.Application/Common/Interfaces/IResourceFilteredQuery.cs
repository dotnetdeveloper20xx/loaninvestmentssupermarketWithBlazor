namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that support resource-based authorization filtering.
/// When a query implements this interface, the ResourceAuthorizationBehaviour will
/// automatically set the filter properties based on the current user's roles.
/// </summary>
public interface IResourceFilteredQuery
{
    /// <summary>
    /// The user ID to filter resources by. Set by the authorization behaviour
    /// for Borrower-only or Lender-only users. Null means no user-level filter (admin access).
    /// </summary>
    string? FilterByUserId { get; set; }

    /// <summary>
    /// The role context for filtering. "Borrower" or "Lender" indicates the type of
    /// ownership filter to apply. Null means no role-based filter (admin access).
    /// </summary>
    string? FilterByRole { get; set; }
}
