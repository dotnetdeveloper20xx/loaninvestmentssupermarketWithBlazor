using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.SimulatePermissions;

/// <summary>
/// Handles the SimulatePermissionsQuery by computing effective permissions for a user.
/// </summary>
public sealed class SimulatePermissionsQueryHandler
    : IRequestHandler<SimulatePermissionsQuery, PermissionSimulationResult>
{
    private readonly IPermissionResolver _permissionResolver;
    private readonly IIdentityService _identityService;

    public SimulatePermissionsQueryHandler(
        IPermissionResolver permissionResolver,
        IIdentityService identityService)
    {
        _permissionResolver = permissionResolver;
        _identityService = identityService;
    }

    public async Task<PermissionSimulationResult> Handle(
        SimulatePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return new PermissionSimulationResult
            {
                UserId = request.UserId,
                UserEmail = string.Empty,
                AssignedRoles = [],
                EffectivePermissions = [],
                EffectivePolicies = []
            };
        }

        var roles = await _identityService.GetUserRolesAsync(request.UserId, cancellationToken);
        var effectivePermissions = await _permissionResolver.GetEffectivePermissionsAsync(request.UserId);

        var permissionDtos = effectivePermissions
            .Select(p =>
            {
                var parts = p.Split('.');
                return new PermissionDto
                {
                    Module = parts.Length > 0 ? parts[0] : string.Empty,
                    Action = parts.Length > 1 ? parts[1] : string.Empty
                };
            })
            .ToList();

        var effectivePolicies = ResolveEffectivePolicies(roles);

        return new PermissionSimulationResult
        {
            UserId = user.Id,
            UserEmail = user.Email ?? string.Empty,
            AssignedRoles = roles,
            EffectivePermissions = permissionDtos,
            EffectivePolicies = effectivePolicies
        };
    }

    /// <summary>
    /// Resolves which authorization policies the user qualifies for based on their roles.
    /// </summary>
    private static IReadOnlyList<string> ResolveEffectivePolicies(IReadOnlyList<string> roles)
    {
        var policies = new List<string>();

        if (roles.Contains("Admin"))
        {
            policies.AddRange([
                "CanManageUsers", "CanProcessApplications", "CanManageProducts",
                "CanViewReports", "CanManageLenders", "CanManageBorrowers",
                "CanApproveProducts", "CanHandleDisputes", "CanManageMessages",
                "CanSetLimits", "CanApproveDisbursements"
            ]);
        }

        if (roles.Contains("CrmManager"))
        {
            policies.AddRange([
                "CanProcessApplications", "CanManageBorrowers", "CanVetUsers",
                "CanApproveProducts", "CanSetLimits"
            ]);
        }

        if (roles.Contains("CustomerService"))
        {
            policies.AddRange(["CanHandleDisputes", "CanManageMessages"]);
        }

        if (roles.Contains("Lender"))
        {
            policies.Add("CanManageProducts");
        }

        if (roles.Contains("Auditor"))
        {
            policies.Add("CanViewReports");
        }

        return policies.Distinct().Order().ToList();
    }
}
