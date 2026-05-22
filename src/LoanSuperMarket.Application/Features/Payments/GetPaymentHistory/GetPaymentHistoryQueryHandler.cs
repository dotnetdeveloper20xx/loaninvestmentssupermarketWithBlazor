using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler
    : IRequestHandler<GetPaymentHistoryQuery, ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>
{
    private readonly ILoanApplicationRepository _repository;

    public GetPaymentHistoryQueryHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>> Handle(
        GetPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new DomainException("Repayment schedule not found.");
        }

        var history = schedule.Installments
            .Where(i => i.PaidAmount > 0)
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new PaymentHistoryItemDto
            {
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate,
                PaidDate = i.PaidDate,
                PaidAmount = i.PaidAmount,
                Status = i.Status.ToString(),
                LateFeeAmount = i.LateFeeAmount
            })
            .ToList();

        return ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>.Ok(
            history,
            "Payment history retrieved successfully.");
    }
}
