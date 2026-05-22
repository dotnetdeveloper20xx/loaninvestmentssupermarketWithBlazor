using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerPaymentSummary;

public sealed class GetBorrowerPaymentSummaryQueryHandler
    : IRequestHandler<GetBorrowerPaymentSummaryQuery, ApiResponse<BorrowerPaymentSummaryDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _loanProductRepository;

    public GetBorrowerPaymentSummaryQueryHandler(
        ILoanApplicationRepository repository,
        IBorrowerRepository borrowerRepository,
        ILoanProductRepository loanProductRepository)
    {
        _repository = repository;
        _borrowerRepository = borrowerRepository;
        _loanProductRepository = loanProductRepository;
    }

    public async Task<ApiResponse<BorrowerPaymentSummaryDto>> Handle(
        GetBorrowerPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByUserIdAsync(
            request.FilterByUserId!, cancellationToken);

        if (borrower is null)
        {
            return ApiResponse<BorrowerPaymentSummaryDto>.Ok(
                new BorrowerPaymentSummaryDto(),
                "No borrower profile found.");
        }

        var schedules = await _repository.GetSchedulesByBorrowerIdAsync(
            borrower.Id, cancellationToken);

        var totalInterestPaid = 0m;
        var totalPrincipalPaid = 0m;
        var paymentHistory = new List<PaymentHistoryEntry>();
        var upcomingPayments = new List<UpcomingPaymentEntry>();
        var threeMonthsFromNow = DateTime.UtcNow.AddMonths(3);

        foreach (var schedule in schedules)
        {
            var productTitle = "Unknown";
            if (schedule.LoanApplication?.LoanProductId is not null)
            {
                var product = await _loanProductRepository.GetByIdAsync(
                    schedule.LoanApplication.LoanProductId.Value, cancellationToken);
                productTitle = product?.Title ?? "Unknown";
            }

            foreach (var installment in schedule.Installments.OrderBy(i => i.InstallmentNumber))
            {
                if (installment.Status == InstallmentStatus.Paid)
                {
                    totalInterestPaid += installment.InterestPortion;
                    totalPrincipalPaid += installment.PrincipalPortion;

                    paymentHistory.Add(new PaymentHistoryEntry
                    {
                        ScheduleId = schedule.Id,
                        ProductTitle = productTitle,
                        InstallmentNumber = installment.InstallmentNumber,
                        DueDate = installment.DueDate,
                        PaidDate = installment.PaidDate,
                        PaidAmount = installment.PaidAmount,
                        Status = installment.Status.ToString()
                    });
                }

                if (installment.Status is InstallmentStatus.Pending or InstallmentStatus.PartiallyPaid
                    && installment.DueDate <= threeMonthsFromNow
                    && installment.DueDate >= DateTime.UtcNow)
                {
                    upcomingPayments.Add(new UpcomingPaymentEntry
                    {
                        ScheduleId = schedule.Id,
                        ProductTitle = productTitle,
                        DueDate = installment.DueDate,
                        Amount = installment.TotalAmount,
                        InstallmentNumber = installment.InstallmentNumber
                    });
                }
            }
        }

        return ApiResponse<BorrowerPaymentSummaryDto>.Ok(new BorrowerPaymentSummaryDto
        {
            TotalInterestPaid = totalInterestPaid,
            TotalPrincipalPaid = totalPrincipalPaid,
            PaymentHistory = paymentHistory.OrderByDescending(p => p.PaidDate).ToList(),
            UpcomingPayments = upcomingPayments.OrderBy(p => p.DueDate).ToList()
        }, "Borrower payment summary retrieved successfully.");
    }
}
