using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that applies resource-based authorization filtering.
/// For queries implementing IResourceFilteredQuery, this behaviour sets filter properties
/// based on the current user's roles to enforce data isolation.
/// </summary>
public sealed class ResourceAuthorizationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;

    private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "CrmManager",
        "Auditor"
    };

    public ResourceAuthorizationBehaviour(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IResourceFilteredQuery filteredQuery)
        {
            return await next(cancellationToken);
        }

        ApplyResourceFilter(filteredQuery);

        return await next(cancellationToken);
    }

    private void ApplyResourceFilter(IResourceFilteredQuery query)
    {
        // If the user is not authenticated, leave filters null (the auth middleware
        // should have already rejected unauthenticated requests for protected endpoints).
        if (!_currentUserService.IsAuthenticated)
        {
            return;
        }

        var roles = _currentUserService.Roles;

        // If the user holds any admin-level role, no resource filter is applied.
        // Admin, CrmManager, and Auditor can see all resources.
        if (roles.Any(role => AdminRoles.Contains(role)))
        {
            query.FilterByUserId = null;
            query.FilterByRole = null;
            return;
        }

        // If the user has ONLY the Borrower role, filter by their user ID with Borrower context.
        if (roles.Any(r => r.Equals("Borrower", StringComparison.OrdinalIgnoreCase)))
        {
            query.FilterByUserId = _currentUserService.UserId;
            query.FilterByRole = "Borrower";
            return;
        }

        // If the user has ONLY the Lender role, filter by their user ID with Lender context.
        if (roles.Any(r => r.Equals("Lender", StringComparison.OrdinalIgnoreCase)))
        {
            query.FilterByUserId = _currentUserService.UserId;
            query.FilterByRole = "Lender";
            return;
        }

        // For any other role combination without admin-level access, apply user-level filter
        // as a safe default.
        query.FilterByUserId = _currentUserService.UserId;
        query.FilterByRole = null;
    }
}
