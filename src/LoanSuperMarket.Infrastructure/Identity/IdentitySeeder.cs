using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Seeds predefined roles and a default Admin account on application startup.
/// Call <see cref="SeedAsync"/> from Program.cs after building the application host.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Role definitions with descriptions matching the design specification.
    /// </summary>
    private static readonly (string Name, string Description)[] PredefinedRoles =
    [
        ("Admin", "Full platform access including user management, system configuration, and final loan disbursement approval"),
        ("CrmManager", "User vetting, registration approval/rejection, loan product approval, credit tier assignment, account limit management, and AML compliance"),
        ("CustomerService", "Handling disputes, messaging moderation, FAQ management, late payment cases, and mediation between borrowers and lenders"),
        ("Lender", "Create loan products, view own products, and see applications submitted against own products"),
        ("Borrower", "Apply for loans, view own applications, and make payments"),
        ("Auditor", "Read-only access to all platform data for compliance purposes")
    ];

    /// <summary>
    /// Permission mappings for each predefined role, defining which module+action combinations are granted.
    /// Admin gets all permissions; other roles get permissions aligned with their policy access.
    /// </summary>
    private static readonly Dictionary<string, (PermissionModule Module, PermissionAction Action)[]> RolePermissions = new()
    {
        ["Admin"] = Enum.GetValues<PermissionModule>()
            .SelectMany(m => Enum.GetValues<PermissionAction>().Select(a => (m, a)))
            .ToArray(),

        ["CrmManager"] =
        [
            (PermissionModule.UserManagement, PermissionAction.View),
            (PermissionModule.UserManagement, PermissionAction.Edit),
            (PermissionModule.UserManagement, PermissionAction.Approve),
            (PermissionModule.LoanManagement, PermissionAction.View),
            (PermissionModule.LoanManagement, PermissionAction.Approve),
            (PermissionModule.ProductManagement, PermissionAction.View),
            (PermissionModule.ProductManagement, PermissionAction.Approve),
            (PermissionModule.FinancialOperations, PermissionAction.View),
            (PermissionModule.FinancialOperations, PermissionAction.Edit),
        ],

        ["CustomerService"] =
        [
            (PermissionModule.UserManagement, PermissionAction.View),
            (PermissionModule.Messaging, PermissionAction.View),
            (PermissionModule.Messaging, PermissionAction.Create),
            (PermissionModule.Messaging, PermissionAction.Edit),
            (PermissionModule.Messaging, PermissionAction.Delete),
        ],

        ["Lender"] =
        [
            (PermissionModule.ProductManagement, PermissionAction.View),
            (PermissionModule.ProductManagement, PermissionAction.Create),
            (PermissionModule.ProductManagement, PermissionAction.Edit),
            (PermissionModule.LoanManagement, PermissionAction.View),
        ],

        ["Borrower"] =
        [
            (PermissionModule.LoanManagement, PermissionAction.View),
            (PermissionModule.LoanManagement, PermissionAction.Create),
        ],

        ["Auditor"] =
        [
            (PermissionModule.UserManagement, PermissionAction.View),
            (PermissionModule.LoanManagement, PermissionAction.View),
            (PermissionModule.ProductManagement, PermissionAction.View),
            (PermissionModule.FinancialOperations, PermissionAction.View),
            (PermissionModule.Reports, PermissionAction.View),
            (PermissionModule.SystemSettings, PermissionAction.View),
            (PermissionModule.Messaging, PermissionAction.View),
        ]
    };

    /// <summary>
    /// Seeds predefined roles and a default Admin account into the Identity store.
    /// This method is idempotent — it only creates roles/users that do not already exist.
    /// </summary>
    /// <param name="serviceProvider">The application's root service provider.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var roleManager = services.GetRequiredService<RoleManager<CustomRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var dbContext = services.GetRequiredService<AuthIdentityDbContext>();
        var logger = services.GetRequiredService<ILogger<AuthIdentityDbContext>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedRolePermissionsAsync(roleManager, dbContext, logger);
        await SeedAdminUserAsync(userManager, configuration, logger);
    }

    private static async Task SeedRolesAsync(
        RoleManager<CustomRole> roleManager,
        ILogger logger)
    {
        foreach (var (name, description) in PredefinedRoles)
        {
            if (await roleManager.RoleExistsAsync(name))
            {
                logger.LogDebug("Role '{RoleName}' already exists, skipping", name);
                continue;
            }

            var role = new CustomRole
            {
                Name = name,
                Description = description,
                IsSystemRole = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "System"
            };

            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                logger.LogInformation("Created predefined role '{RoleName}'", name);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create role '{RoleName}': {Errors}", name, errors);
            }
        }
    }

    private static async Task SeedRolePermissionsAsync(
        RoleManager<CustomRole> roleManager,
        AuthIdentityDbContext dbContext,
        ILogger logger)
    {
        foreach (var (roleName, permissions) in RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                logger.LogWarning("Role '{RoleName}' not found when seeding permissions, skipping", roleName);
                continue;
            }

            var existingPermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            foreach (var (module, action) in permissions)
            {
                var alreadyExists = existingPermissions.Any(ep =>
                    ep.Module == module && ep.Action == action);

                if (alreadyExists)
                    continue;

                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Module = module,
                    Action = action,
                    GrantedAtUtc = DateTime.UtcNow,
                    GrantedBy = "System"
                });
            }
        }

        var changes = await dbContext.SaveChangesAsync();
        if (changes > 0)
        {
            logger.LogInformation("Seeded {Count} role permission(s)", changes);
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Check if any user with Admin role already exists
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count > 0)
        {
            logger.LogDebug("Admin user already exists, skipping admin seed");
            return;
        }

        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "AdminSeed:Email or AdminSeed:Password not configured in appsettings. " +
                "Skipping default admin account creation");
            return;
        }

        // Check if user with this email already exists (might exist without Admin role)
        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is not null)
        {
            var addRoleResult = await userManager.AddToRoleAsync(existingUser, "Admin");
            if (addRoleResult.Succeeded)
            {
                logger.LogInformation("Assigned Admin role to existing user '{Email}'", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to assign Admin role to '{Email}': {Errors}", adminEmail, errors);
            }
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            AccountStatus = AccountStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to create default admin user: {Errors}", errors);
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (roleResult.Succeeded)
        {
            logger.LogInformation("Created default admin account '{Email}' with Admin role", adminEmail);
        }
        else
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to assign Admin role to new admin user: {Errors}", errors);
        }
    }
}
