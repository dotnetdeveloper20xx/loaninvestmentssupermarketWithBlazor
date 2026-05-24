using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderLoans;

public sealed class GetLenderLoansQueryHandler
    : IRequestHandler<GetLenderLoansQuery, ApiResponse<IReadOnlyList<LenderLoanDto>>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public GetLenderLoansQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository,
        IBorrowerRepository borrowerRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<LenderLoanDto>>> Handle(
        GetLenderLoansQuery request,
        CancellationToken cancellationToken)
    {
        var lender = await _lenderRepository.GetByUserIdAsync(
            request.FilterByUserId!, cancellationToken);

        if (lender is null)
        {
            return ApiResponse<IReadOnlyList<LenderLoanDto>>.Ok(
                Array.Empty<LenderLoanDto>(),
                "No lender profile found.");
        }

        var schedules = await _repository.GetSchedulesByLenderIdAsync(lender.Id, cancellationToken);

        var loans = new List<LenderLoanDto>();

        foreach (var schedule in schedules)
        {
            if (!string.IsNullOrWhiteSpace(request.PerformanceFilter)
                && Enum.TryParse<LoanPerformance>(request.PerformanceFilter, out var perfFilter)
                && schedule.Performance != perfFilter)
            {
                continue;
            }

            var borrowerName = "Unknown";
            if (schedule.LoanApplication is not null)
            {
                var borrower = await _borrowerRepository.GetByIdAsync(
                    schedule.LoanApplication.BorrowerId, cancellationToken);
                if (borrower is not null)
                {
                    borrowerName = $"{borrower.FirstName} {borrower.LastName}";
                }
            }

            var nextInstallment = schedule.GetNextPendingInstallment();

            loans.Add(new LenderLoanDto
            {
                ScheduleId = schedule.Id,
                BorrowerName = borrowerName,
                FundedAmount = schedule.FundedAmount,
                TermMonths = schedule.TermMonths,
                EffectiveRate = schedule.AnnualInterestRate,
                Performance = schedule.Performance.ToString(),
                NextDueDate = nextInstallment?.DueDate
            });
        }

        IEnumerable<LenderLoanDto> sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "amount" => loans.OrderByDescending(l => l.FundedAmount),
            "duedate" => loans.OrderBy(l => l.NextDueDate),
            "performance" => loans.OrderBy(l => l.Performance),
            _ => loans.OrderByDescending(l => l.FundedAmount)
        };

        return ApiResponse<IReadOnlyList<LenderLoanDto>>.Ok(
            sorted.ToList(),
            "Lender loans retrieved successfully.");
    }
}
