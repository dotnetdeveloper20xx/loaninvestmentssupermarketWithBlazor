using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;

public sealed class GetLenderDashboardQueryHandler
    : IRequestHandler<GetLenderDashboardQuery, ApiResponse<LenderPortfolioDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;

    public GetLenderDashboardQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
    }

    public async Task<ApiResponse<LenderPortfolioDto>> Handle(
        GetLenderDashboardQuery request,
        CancellationToken cancellationToken)
    {
        // Get lender by user ID
        var lenders = await _lenderRepository.GetAllAsync(cancellationToken);
        var lender = lenders.FirstOrDefault(l => l.UserId == request.FilterByUserId);

        if (lender is null)
        {
            return ApiResponse<LenderPortfolioDto>.Ok(new LenderPortfolioDto(),
                "No lender profile found.");
        }

        var schedules = await _repository.GetSchedulesByLenderIdAsync(lender.Id, cancellationToken);

        var totalFunded = schedules.Sum(s => s.FundedAmount);
        var activeLoans = schedules.Count(s => s.Performance != LoanPerformance.Defaulted);
        var outstandingPrincipal = schedules.Sum(s =>
            s.FundedAmount - s.Installments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.PrincipalPortion));
        var expectedMonthlyIncome = schedules
            .Where(s => s.Performance != LoanPerformance.Defaulted)
            .Sum(s => s.MonthlyEmi);
        var defaultRate = schedules.Count > 0
            ? (decimal)schedules.Count(s => s.Performance == LoanPerformance.Defaulted) / schedules.Count * 100
            : 0;

        return ApiResponse<LenderPortfolioDto>.Ok(new LenderPortfolioDto
        {
            TotalFunded = totalFunded,
            ActiveLoanCount = activeLoans,
            OutstandingPrincipal = outstandingPrincipal,
            ExpectedMonthlyIncome = expectedMonthlyIncome,
            DefaultRate = decimal.Round(defaultRate, 2),
            AvailableFunds = lender.AvailableFunds
        }, "Lender portfolio retrieved successfully.");
    }
}
