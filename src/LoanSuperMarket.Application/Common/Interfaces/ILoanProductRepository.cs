using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.LoanProducts;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILoanProductRepository
{
    Task AddAsync(LoanProduct loanProduct, CancellationToken cancellationToken);

    Task<LoanProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanProduct>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PagedResult<LoanProductDto>> GetPagedAsync(
    GridQueryRequest request,
    CancellationToken cancellationToken);
}