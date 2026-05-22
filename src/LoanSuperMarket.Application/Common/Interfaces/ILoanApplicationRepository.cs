using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Funding;
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

    /// <summary>
    /// Gets approved applications available for funding by a specific lender.
    /// </summary>
    Task<IReadOnlyList<FundingQueueItemDto>> GetFundingQueueAsync(
        string? lenderUserId,
        string? productTitleFilter,
        decimal? minAmount,
        decimal? maxAmount,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new repayment schedule with its installments.
    /// </summary>
    Task AddRepaymentScheduleAsync(RepaymentSchedule schedule, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a repayment schedule by ID including all installments.
    /// </summary>
    Task<RepaymentSchedule?> GetRepaymentScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all repayment schedules for a specific lender.
    /// </summary>
    Task<IReadOnlyList<RepaymentSchedule>> GetSchedulesByLenderIdAsync(
        Guid lenderId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all repayment schedules for a specific borrower (via loan application).
    /// </summary>
    Task<IReadOnlyList<RepaymentSchedule>> GetSchedulesByBorrowerIdAsync(
        Guid borrowerId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}