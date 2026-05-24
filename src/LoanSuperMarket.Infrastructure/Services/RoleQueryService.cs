using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Services;

public sealed class RoleQueryService : IRoleQueryService
{
    private readonly AuthIdentityDbContext _context;
    private readonly RoleManager<CustomRole> _roleManager;

    public RoleQueryService(
        AuthIdentityDbContext context,
        RoleManager<CustomRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                CreatedAtUtc = role.CreatedAtUtc,
                CreatedBy = role.CreatedBy,
                UserCount = userCount
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new PermissionDto
            {
                Module = rp.Module.ToString(),
                Action = rp.Action.ToString()
            })
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
