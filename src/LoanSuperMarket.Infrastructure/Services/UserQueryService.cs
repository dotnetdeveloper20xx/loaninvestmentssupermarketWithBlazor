using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Infrastructure.Identity;
using LoanSuperMarket.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Services;

public sealed class UserQueryService : IUserQueryService
{
    private readonly AuthIdentityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserQueryService(
        AuthIdentityDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(u =>
                u.Email!.Contains(term) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
            var userIds = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => userIds.Contains(u.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Roles = roles.ToList(),
                AccountStatus = user.AccountStatus.ToString(),
                LastLoginAtUtc = user.LastLoginAtUtc,
                CreatedAtUtc = user.CreatedAtUtc
            });
        }

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Roles = roles.ToList(),
            AccountStatus = user.AccountStatus.ToString(),
            AccountStatusReason = user.AccountStatusReason,
            AccountStatusChangedAtUtc = user.AccountStatusChangedAtUtc,
            AccountStatusChangedBy = user.AccountStatusChangedBy,
            CreditTier = user.CreditTier?.ToString(),
            CreditLimit = user.CreditLimit,
            CapitalLimit = user.CapitalLimit,
            BlockedActivity = user.BlockedActivity,
            TwoFactorEnabled = user.TwoFactorEnabled,
            EmailConfirmed = user.EmailConfirmed,
            LastLoginAtUtc = user.LastLoginAtUtc,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public async Task<PagedResult<VettingItemDto>> GetVettingQueueAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.AccountStatus == Domain.Enums.AccountStatus.PendingApproval);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<VettingItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userType = roles.Contains("Lender") ? "Lender" :
                           roles.Contains("Borrower") ? "Borrower" : "Unknown";

            items.Add(new VettingItemDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                UserType = userType,
                EmailConfirmed = user.EmailConfirmed,
                RegisteredAtUtc = user.CreatedAtUtc
            });
        }

        return new PagedResult<VettingItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }
}
