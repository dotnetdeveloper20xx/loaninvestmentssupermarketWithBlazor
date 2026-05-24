using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetInvestorAnalytics;

public sealed class GetInvestorAnalyticsQueryHandler
    : IRequestHandler<GetInvestorAnalyticsQuery, ApiResponse<InvestorAnalyticsDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public GetInvestorAnalyticsQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository,
        IBorrowerRepository borrowerRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ApiResponse<InvestorAnalyticsDto>> Handle(
        GetInvestorAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var lender = await _lenderRepository.GetByUserIdAsync(
            request.FilterByUserId!, cancellationToken);

        if (lender is null)
        {
            return ApiResponse<InvestorAnalyticsDto>.Ok(
                new InvestorAnalyticsDto(), "No lender profile found.");
        }

        var schedules = await _repository.GetSchedulesByLenderIdAsync(
            lender.Id, cancellationToken);

        var totalInvested = schedules.Sum(s => s.FundedAmount);
        var totalLoans = schedules.Count;
        var performingLoans = schedules.Count(s => s.Performance == LoanPerformance.OnTime);
        var lateLoans = schedules.Count(s => s.Performance == LoanPerformance.Late);
        var defaultedLoans = schedules.Count(s => s.Performance == LoanPerformance.Defaulted);

        var loanBreakdown = new List<LoanRoiDto>();
        var totalReturned = 0m;

        foreach (var schedule in schedules)
        {
            var interestEarned = schedule.Installments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.InterestPortion);

            var lateFeesEarned = schedule.Installments
                .Where(i => i.Status == InstallmentStatus.Paid && i.LateFeeAmount > 0)
                .Sum(i => i.LateFeeAmount);

            var principalReturned = schedule.Installments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.PrincipalPortion);

            var totalReturn = interestEarned + lateFeesEarned;
            totalReturned += principalReturned + totalReturn;

            var roi = schedule.FundedAmount > 0
                ? totalReturn / schedule.FundedAmount * 100
                : 0;

            var borrowerName = "Unknown";
            if (schedule.LoanApplication is not null)
            {
                var borrower = await _borrowerRepository.GetByIdAsync(
                    schedule.LoanApplication.BorrowerId, cancellationToken);
                if (borrower is not null)
                    borrowerName = $"{borrower.FirstName} {borrower.LastName}";
            }

            loanBreakdown.Add(new LoanRoiDto
            {
                ScheduleId = schedule.Id,
                BorrowerName = borrowerName,
                FundedAmount = schedule.FundedAmount,
                InterestEarned = interestEarned,
                LateFeesEarned = lateFeesEarned,
                TotalReturn = totalReturn,
                RoiPercentage = decimal.Round(roi, 2),
                Performance = schedule.Performance.ToString()
            });
        }

        var netProfit = totalReturned - totalInvested;
        var averageRoi = totalLoans > 0
            ? loanBreakdown.Average(l => l.RoiPercentage)
            : 0;

        // Simple diversification score: 1 - HHI (Herfindahl index)
        var diversificationScore = 0m;
        if (totalInvested > 0 && totalLoans > 1)
        {
            var hhi = schedules.Sum(s =>
            {
                var share = s.FundedAmount / totalInvested;
                return share * share;
            });
            diversificationScore = decimal.Round((1 - hhi) * 100, 1);
        }

        // Annualized yield estimate
        var oldestSchedule = schedules.MinBy(s => s.GeneratedAtUtc);
        var monthsActive = oldestSchedule is not null
            ? Math.Max(1, (DateTime.UtcNow - oldestSchedule.GeneratedAtUtc).Days / 30.0)
            : 1;
        var annualizedYield = totalInvested > 0
            ? decimal.Round((decimal)((double)(totalReturned - totalInvested) / (double)totalInvested / monthsActive * 12 * 100), 2)
            : 0;

        return ApiResponse<InvestorAnalyticsDto>.Ok(new InvestorAnalyticsDto
        {
            TotalInvested = totalInvested,
            TotalReturned = totalReturned,
            NetProfit = netProfit,
            AnnualizedYield = annualizedYield,
            AverageRoi = decimal.Round(averageRoi, 2),
            TotalLoans = totalLoans,
            PerformingLoans = performingLoans,
            LateLoans = lateLoans,
            DefaultedLoans = defaultedLoans,
            DiversificationScore = diversificationScore,
            LoanBreakdown = loanBreakdown.OrderByDescending(l => l.RoiPercentage).ToList()
        }, "Investor analytics retrieved successfully.");
    }
}
