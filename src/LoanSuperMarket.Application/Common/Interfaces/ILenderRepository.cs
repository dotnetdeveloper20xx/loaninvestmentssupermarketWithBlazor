using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.Lenders;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILenderRepository
{
    Task AddAsync(Lender lender, CancellationToken cancellationToken);

    Task<Lender?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Lender?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Lender>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<PagedResult<LenderDto>> GetPagedAsync(
    GridQueryRequest request,
    CancellationToken cancellationToken);
}