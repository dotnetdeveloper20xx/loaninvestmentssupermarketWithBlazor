using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderEarnings;

public sealed class GetLenderEarningsQueryHandler
    : IRequestHandler<GetLenderEarningsQuery, ApiResponse<LenderEarningsDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;

    public GetLenderEarningsQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
    }

    public async Task<ApiResponse<LenderEarningsDto>> Handle(
        GetLenderEarningsQuery request,
        CancellationToken cancellationToken)
    {
        var lender = await _lenderRepository.GetByUserIdAsync(
            request.FilterByUserId!, cancellationToken);

        if (lender is null)
        {
            return ApiResponse<LenderEarningsDto>.Ok(new LenderEarningsDto(),
                "No lender profile found.");
        }

        var schedules = await _repository.GetSchedulesByLenderIdAsync(lender.Id, cancellationToken);

        var totalInterestReceived = schedules.Sum(s =>
            s.Installments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.InterestPortion));

        var projectedTotalReturns = schedules.Sum(s => s.TotalInterestPayable);

        var totalLateFeesCollected = schedules.Sum(s =>
            s.Installments
                .Where(i => i.Status == InstallmentStatus.Paid && i.LateFeeAmount > 0)
                .Sum(i => i.LateFeeAmount));

        return ApiResponse<LenderEarningsDto>.Ok(new LenderEarningsDto
        {
            TotalInterestReceived = totalInterestReceived,
            ProjectedTotalReturns = projectedTotalReturns,
            TotalLateFeesCollected = totalLateFeesCollected,
            AvailableFunds = lender.AvailableFunds
        }, "Lender earnings retrieved successfully.");
    }
}
