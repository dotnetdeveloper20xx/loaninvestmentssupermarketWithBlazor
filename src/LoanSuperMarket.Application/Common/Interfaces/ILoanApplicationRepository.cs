using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.LoanApplications;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILoanApplicationRepository
{
    Task AddAsync(LoanApplication application, CancellationToken cancellationToken);

    Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<LoanApplication?> GetByIdWithDocumentsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LoanApplication>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WizardApplicationSummaryDto>> GetByBorrowerIdAsync(
        Guid borrowerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewQueueItemDto>> GetReviewQueueAsync(
        LoanApplicationStatus[]? statusFilter, string? sortBy, CancellationToken cancellationToken);

    /// <summary>
    /// Counts active loan applications for the specified borrower.
    /// Active statuses include: Submitted, UnderReview, Approved, and Funded.
    /// </summary>
    Task<int> CountActiveByBorrowerIdAsync(Guid borrowerId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}