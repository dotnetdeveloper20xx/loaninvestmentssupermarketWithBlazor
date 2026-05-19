using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILoanProductRepository
{
    Task AddAsync(LoanProduct loanProduct, CancellationToken cancellationToken);

    Task<LoanProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanProduct>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}