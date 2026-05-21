using Microsoft.AspNetCore.Authorization;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Configures named authorization policies that map roles to specific platform capabilities.
/// Each policy defines which roles are permitted to access the associated endpoints.
/// </summary>
public static class AuthorizationPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanProcessApplications = nameof(CanProcessApplications);
    public const string CanManageProducts = nameof(CanManageProducts);
    public const string CanViewReports = nameof(CanViewReports);
    public const string CanManageLenders = nameof(CanManageLenders);
    public const string CanManageBorrowers = nameof(CanManageBorrowers);
    public const string CanVetUsers = nameof(CanVetUsers);
    public const string CanApproveProducts = nameof(CanApproveProducts);
    public const string CanHandleDisputes = nameof(CanHandleDisputes);
    public const string CanManageMessages = nameof(CanManageMessages);
    public const string CanSetLimits = nameof(CanSetLimits);
    public const string CanApproveDisbursements = nameof(CanApproveDisbursements);

    /// <summary>
    /// Registers all named authorization policies with their required role mappings.
    /// </summary>
    /// <param name="options">The authorization options to configure.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageUsers, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(CanProcessApplications, policy =>
            policy.RequireRole("Admin", "CrmManager"));

        options.AddPolicy(CanManageProducts, policy =>
            policy.RequireRole("Admin", "CrmManager", "Lender"));

        options.AddPolicy(CanViewReports, policy =>
            policy.RequireRole("Admin", "Auditor"));

        options.AddPolicy(CanManageLenders, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(CanManageBorrowers, policy =>
            policy.RequireRole("Admin", "CrmManager"));

        options.AddPolicy(CanVetUsers, policy =>
            policy.RequireRole("CrmManager"));

        options.AddPolicy(CanApproveProducts, policy =>
            policy.RequireRole("CrmManager", "Admin"));

        options.AddPolicy(CanHandleDisputes, policy =>
            policy.RequireRole("CustomerService", "Admin"));

        options.AddPolicy(CanManageMessages, policy =>
            policy.RequireRole("CustomerService", "Admin"));

        options.AddPolicy(CanSetLimits, policy =>
            policy.RequireRole("CrmManager", "Admin"));

        options.AddPolicy(CanApproveDisbursements, policy =>
            policy.RequireRole("Admin"));
    }
}
