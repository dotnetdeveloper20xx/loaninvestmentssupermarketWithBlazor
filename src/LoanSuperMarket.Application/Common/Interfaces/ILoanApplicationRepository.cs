using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILoanApplicationRepository
{
    Task AddAsync(LoanApplication application, CancellationToken cancellationToken);

    Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanApplication>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Counts active loan applications for the specified borrower.
    /// Active statuses include: Submitted, UnderReview, Approved, and Funded.
    /// </summary>
    Task<int> CountActiveByBorrowerIdAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}