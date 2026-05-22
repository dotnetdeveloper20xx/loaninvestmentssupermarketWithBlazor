using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Services;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.RecordPayment;

public sealed class RecordPaymentCommandHandler
    : IRequestHandler<RecordPaymentCommand, ApiResponse<PaymentResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IPaymentProcessor _paymentProcessor;

    public RecordPaymentCommandHandler(
        ILoanApplicationRepository repository,
        IPaymentProcessor paymentProcessor)
    {
        _repository = repository;
        _paymentProcessor = paymentProcessor;
    }

    public async Task<ApiResponse<PaymentResultDto>> Handle(
        RecordPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new DomainException("Repayment schedule not found.");
        }

        _paymentProcessor.RecordPayment(schedule, request.Amount, request.PaymentDate);

        await _repository.SaveChangesAsync(cancellationToken);

        var paidInstallment = schedule.Installments
            .OrderByDescending(i => i.PaidDate)
            .First();

        var totalOwed = paidInstallment.TotalAmount + paidInstallment.LateFeeAmount;

        return ApiResponse<PaymentResultDto>.Ok(new PaymentResultDto
        {
            InstallmentNumber = paidInstallment.InstallmentNumber,
            Status = paidInstallment.Status.ToString(),
            PaidAmount = paidInstallment.PaidAmount,
            RemainingOnInstallment = totalOwed - paidInstallment.PaidAmount,
            TotalPaidToDate = schedule.GetTotalPaidToDate()
        }, "Payment recorded successfully.");
    }
}
