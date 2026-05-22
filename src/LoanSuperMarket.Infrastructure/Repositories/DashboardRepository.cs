using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken)
    {
        var totalBorrowers = await _context.Borrowers.CountAsync(cancellationToken);

        var totalLenders = await _context.Lenders.CountAsync(cancellationToken);

        var totalLoanProducts = await _context.LoanProducts.CountAsync(cancellationToken);

        var totalApplications = await _context.LoanApplications.CountAsync(cancellationToken);

        var approvedApplications = await _context.LoanApplications
            .CountAsync(
                x => x.Status == LoanApplicationStatus.Approved,
                cancellationToken);

        var fundedApplications = await _context.LoanApplications
            .CountAsync(
                x => x.Status == LoanApplicationStatus.Funded,
                cancellationToken);

        var totalFundingVolume = await _context.LoanApplications
            .Where(x => x.Status == LoanApplicationStatus.Funded)
            .SumAsync(
                x => (decimal?)x.RequestedAmount.Amount,
                cancellationToken) ?? 0;

        var approvalRate = totalApplications == 0
            ? 0
            : Math.Round((decimal)approvedApplications / totalApplications * 100, 2);

        var fundingRate = totalApplications == 0
            ? 0
            : Math.Round((decimal)fundedApplications / totalApplications * 100, 2);

        var recentApplications = await _context.LoanApplications
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Take(5)
            .Select(x => new RecentApplicationDto
            {
                Id = x.Id,
                Purpose = x.Purpose,
                Amount = x.RequestedAmount.Amount,
                Status = x.Status.ToString(),
                SubmittedAtUtc = x.SubmittedAtUtc ?? DateTime.MinValue
            })
            .ToListAsync(cancellationToken);

        var recentBorrowers = await _context.Borrowers
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new RecentBorrowerDto
            {
                Id = x.Id,
                FullName = x.FirstName + " " + x.LastName,
                Email = x.Email,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalBorrowers = totalBorrowers,
            TotalLenders = totalLenders,
            TotalLoanProducts = totalLoanProducts,
            TotalApplications = totalApplications,
            ApprovedApplications = approvedApplications,
            FundedApplications = fundedApplications,
            TotalFundingVolume = totalFundingVolume,
            ApprovalRate = approvalRate,
            FundingRate = fundingRate,
            RecentApplications = recentApplications,
            RecentBorrowers = recentBorrowers
        };
    }
}