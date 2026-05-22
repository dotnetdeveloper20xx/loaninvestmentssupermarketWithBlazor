using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.LoanApplications;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class LoanApplicationRepository : ILoanApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public LoanApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoanApplication application, CancellationToken cancellationToken)
    {
        await _context.LoanApplications.AddAsync(application, cancellationToken);
    }

    public async Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<LoanApplication?> GetByIdWithDocumentsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LoanApplication>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WizardApplicationSummaryDto>> GetByBorrowerIdAsync(
        Guid borrowerId, CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .Where(x => x.BorrowerId == borrowerId)
            .Include(x => x.Documents)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WizardApplicationSummaryDto(
                x.Id,
                x.LoanProductId != null
                    ? _context.LoanProducts
                        .Where(p => p.Id == x.LoanProductId)
                        .Select(p => p.Title)
                        .FirstOrDefault()
                    : null,
                x.RequestedAmount.Amount,
                x.TermMonths,
                x.SubmittedAtUtc,
                (int)x.Status,
                0, // MatchedProductCount — computed client-side or via separate query
                x.Documents.Count,
                x.Documents.Count(d => d.Status == DocumentStatus.Verified),
                x.Documents.Count(d => d.Status == DocumentStatus.Rejected)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewQueueItemDto>> GetReviewQueueAsync(
        LoanApplicationStatus[]? statusFilter, string? sortBy, CancellationToken cancellationToken)
    {
        var query = _context.LoanApplications
            .Include(x => x.Documents)
            .AsQueryable();

        if (statusFilter is { Length: > 0 })
        {
            query = query.Where(x => statusFilter.Contains(x.Status));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "amount" => query.OrderByDescending(x => x.RequestedAmount.Amount),
            "status" => query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.SubmittedAtUtc)
        };

        return await query
            .Select(x => new ReviewQueueItemDto(
                x.Id,
                _context.Borrowers
                    .Where(b => b.Id == x.BorrowerId)
                    .Select(b => b.FirstName + " " + b.LastName)
                    .FirstOrDefault() ?? "Unknown",
                x.RequestedAmount.Amount,
                x.LoanProductId != null
                    ? _context.LoanProducts
                        .Where(p => p.Id == x.LoanProductId)
                        .Select(p => p.Title)
                        .FirstOrDefault() ?? "No product"
                    : "No product",
                x.SubmittedAtUtc ?? x.CreatedAtUtc,
                (int)x.Status,
                x.Documents.Count,
                x.Documents.Count(d => d.Status == DocumentStatus.Verified)))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByBorrowerIdAsync(Guid borrowerId, CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .CountAsync(x => x.BorrowerId == borrowerId
                && (x.Status == LoanApplicationStatus.Submitted
                    || x.Status == LoanApplicationStatus.UnderReview
                    || x.Status == LoanApplicationStatus.Approved
                    || x.Status == LoanApplicationStatus.Funded),
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
