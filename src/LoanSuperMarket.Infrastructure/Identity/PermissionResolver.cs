using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Computes effective permissions for a user as the union of all permissions
/// from their assigned roles (predefined + custom). Predefined roles have
/// default permissions defined in a static dictionary; custom roles derive
/// permissions from the RolePermissions table.
/// </summary>
public sealed class PermissionResolver : IPermissionResolver
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<CustomRole> _roleManager;
    private readonly AuthIdentityDbContext _dbContext;

    /// <summary>
    /// Default permissions for predefined system roles.
    /// Each role maps to a set of Module.Action permission strings.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> PredefinedRolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = BuildAllPermissions(),
            ["CrmManager"] = new List<string>
            {
                "UserManagement.View",
                "UserManagement.Edit",
                "UserManagement.Approve",
                "LoanManagement.View",
                "LoanManagement.Edit",
                "LoanManagement.Approve",
                "ProductManagement.View",
                "ProductManagement.Edit",
                "ProductManagement.Approve",
                "FinancialOperations.View",
                "FinancialOperations.Edit",
                "Reports.View",
                "Messaging.View",
                "Messaging.Create",
            },
            ["CustomerService"] = new List<string>
            {
                "UserManagement.View",
                "LoanManagement.View",
                "Messaging.View",
                "Messaging.Create",
                "Messaging.Edit",
            },
            ["Lender"] = new List<string>
            {
                "ProductManagement.View",
                "ProductManagement.Create",
                "ProductManagement.Edit",
                "LoanManagement.View",
                "FinancialOperations.View",
            },
            ["Borrower"] = new List<string>
            {
                "LoanManagement.View",
                "LoanManagement.Create",
                "FinancialOperations.View",
            },
            ["Auditor"] = new List<string>
            {
                "UserManagement.View",
                "LoanManagement.View",
                "ProductManagement.View",
                "FinancialOperations.View",
                "Reports.View",
                "SystemSettings.View",
                "Messaging.View",
            },
        };

    public PermissionResolver(
        UserManager<ApplicationUser> userManager,
        RoleManager<CustomRole> roleManager,
        AuthIdentityDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        return await ComputePermissionsForRolesAsync(roleNames);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SimulatePermissionsAsync(
        string userId,
        IReadOnlyList<string> additionalRoles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Combine current roles with additional simulated roles
        var allRoles = currentRoles
            .Concat(additionalRoles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return await ComputePermissionsForRolesAsync(allRoles);
    }

    /// <summary>
    /// Computes the union of all permissions for the given set of role names.
    /// For predefined roles, uses the static default permissions dictionary.
    /// For custom roles, queries the RolePermissions table.
    /// </summary>
    private async Task<IReadOnlyList<string>> ComputePermissionsForRolesAsync(IEnumerable<string> roleNames)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var roleNameList = roleNames.ToList();
        var customRoleNames = new List<string>();

        // Collect permissions from predefined roles
        foreach (var roleName in roleNameList)
        {
            if (PredefinedRolePermissions.TryGetValue(roleName, out var predefinedPermissions))
            {
                foreach (var permission in predefinedPermissions)
                {
                    permissions.Add(permission);
                }
            }
            else
            {
                // Not a predefined role — treat as custom
                customRoleNames.Add(roleName);
            }
        }

        // Query RolePermissions table for custom roles
        if (customRoleNames.Count > 0)
        {
            var customRoleIds = await _roleManager.Roles
                .Where(r => customRoleNames.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync();

            if (customRoleIds.Count > 0)
            {
                var customPermissions = await _dbContext.RolePermissions
                    .Where(rp => customRoleIds.Contains(rp.RoleId))
                    .Select(rp => new { rp.Module, rp.Action })
                    .ToListAsync();

                foreach (var cp in customPermissions)
                {
                    permissions.Add($"{cp.Module}.{cp.Action}");
                }
            }
        }

        return permissions.OrderBy(p => p).ToList().AsReadOnly();
    }

    /// <summary>
    /// Builds the complete set of all possible permissions (all Module.Action combinations).
    /// Used for the Admin role which has full access.
    /// </summary>
    private static IReadOnlyList<string> BuildAllPermissions()
    {
        var allPermissions = new List<string>();

        foreach (var module in Enum.GetValues<PermissionModule>())
        {
            foreach (var action in Enum.GetValues<PermissionAction>())
            {
                allPermissions.Add($"{module}.{action}");
            }
        }

        return allPermissions.AsReadOnly();
    }
}
