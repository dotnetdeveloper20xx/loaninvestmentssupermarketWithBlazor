using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Query service for user-related read operations that require direct database access
/// with pagination, search, and filtering capabilities.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// Gets a paged list of users with optional search and role filter.
    /// </summary>
    Task<PagedResult<UserDto>> GetUsersPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed user information by user ID, including roles, status, and credit info.
    /// </summary>
    Task<UserDetailDto?> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of users pending approval (vetting queue), sorted by registration date.
    /// </summary>
    Task<PagedResult<VettingItemDto>> GetVettingQueueAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
