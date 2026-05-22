using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerLoans;

public sealed class GetBorrowerLoansQueryHandler
    : IRequestHandler<GetBorrowerLoansQuery, ApiResponse<IReadOnlyList<BorrowerLoanDto>>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _loanProductRepository;

    public GetBorrowerLoansQueryHandler(
        ILoanApplicationRepository repository,
        IBorrowerRepository borrowerRepository,
        ILoanProductRepository loanProductRepository)
    {
        _repository = repository;
        _borrowerRepository = borrowerRepository;
        _loanProductRepository = loanProductRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<BorrowerLoanDto>>> Handle(
        GetBorrowerLoansQuery request,
        CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByUserIdAsync(
            request.FilterByUserId!, cancellationToken);

        if (borrower is null)
        {
            return ApiResponse<IReadOnlyList<BorrowerLoanDto>>.Ok(
                Array.Empty<BorrowerLoanDto>(),
                "No borrower profile found.");
        }

        var schedules = await _repository.GetSchedulesByBorrowerIdAsync(
            borrower.Id, cancellationToken);

        var loans = new List<BorrowerLoanDto>();
        var reminderDays = 3;

        foreach (var schedule in schedules)
        {
            var productTitle = "Unknown";
            if (schedule.LoanApplication?.LoanProductId is not null)
            {
                var product = await _loanProductRepository.GetByIdAsync(
                    schedule.LoanApplication.LoanProductId.Value, cancellationToken);
                productTitle = product?.Title ?? "Unknown";
            }

            var nextInstallment = schedule.GetNextPendingInstallment();
            var paidCount = schedule.Installments.Count(i => i.Status == InstallmentStatus.Paid);
            var totalCount = schedule.Installments.Count;
            var progressPercentage = totalCount > 0
                ? decimal.Round((decimal)paidCount / totalCount * 100, 1)
                : 0;

            var isDueSoon = nextInstallment is not null
                && (nextInstallment.DueDate - DateTime.UtcNow).TotalDays <= reminderDays
                && nextInstallment.DueDate >= DateTime.UtcNow;

            var hasLateOrMissed = schedule.Installments
                .Any(i => i.Status is InstallmentStatus.Late or InstallmentStatus.Missed);

            loans.Add(new BorrowerLoanDto
            {
                ScheduleId = schedule.Id,
                ProductTitle = productTitle,
                FundedAmount = schedule.FundedAmount,
                TermMonths = schedule.TermMonths,
                EffectiveRate = schedule.AnnualInterestRate,
                NextDueDate = nextInstallment?.DueDate,
                NextAmount = nextInstallment?.TotalAmount,
                PaidCount = paidCount,
                TotalCount = totalCount,
                ProgressPercentage = progressPercentage,
                IsDueSoon = isDueSoon,
                HasLateOrMissed = hasLateOrMissed
            });
        }

        return ApiResponse<IReadOnlyList<BorrowerLoanDto>>.Ok(
            loans,
            "Borrower loans retrieved successfully.");
    }
}
