using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.Services;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.RecordBulkPayment;

public sealed class RecordBulkPaymentCommandHandler
    : IRequestHandler<RecordBulkPaymentCommand, ApiResponse<BulkPaymentResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IPaymentProcessor _paymentProcessor;

    public RecordBulkPaymentCommandHandler(
        ILoanApplicationRepository repository,
        IPaymentProcessor paymentProcessor)
    {
        _repository = repository;
        _paymentProcessor = paymentProcessor;
    }

    public async Task<ApiResponse<BulkPaymentResultDto>> Handle(
        RecordBulkPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new DomainException("Repayment schedule not found.");
        }

        var installmentsPaid = _paymentProcessor.RecordBulkPayment(
            schedule, request.Amount, request.PaymentDate);

        await _repository.SaveChangesAsync(cancellationToken);

        var totalPaid = schedule.GetTotalPaidToDate();
        var totalOwed = schedule.Installments.Sum(i => i.TotalAmount + i.LateFeeAmount);
        var isFullyPaid = schedule.GetNextPendingInstallment() is null;

        return ApiResponse<BulkPaymentResultDto>.Ok(new BulkPaymentResultDto
        {
            InstallmentsPaid = installmentsPaid,
            TotalAmountApplied = request.Amount,
            RemainingOnSchedule = totalOwed - totalPaid,
            TotalPaidToDate = totalPaid,
            IsFullyPaidOff = isFullyPaid
        }, $"Bulk payment applied. {installmentsPaid} installment(s) fully paid.");
    }
}
