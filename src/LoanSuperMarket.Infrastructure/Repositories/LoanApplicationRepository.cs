using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Infrastructure.Persistence;
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

    public async Task<IReadOnlyList<LoanApplication>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.LoanApplications
            .OrderByDescending(x => x.SubmittedAtUtc)
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