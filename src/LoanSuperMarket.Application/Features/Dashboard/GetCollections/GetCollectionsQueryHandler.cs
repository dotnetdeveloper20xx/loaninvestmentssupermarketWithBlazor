using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetCollections;

public sealed class GetCollectionsQueryHandler
    : IRequestHandler<GetCollectionsQuery, ApiResponse<IReadOnlyList<CollectionItemDto>>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public GetCollectionsQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository,
        IBorrowerRepository borrowerRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<CollectionItemDto>>> Handle(
        GetCollectionsQuery request,
        CancellationToken cancellationToken)
    {
        var lenders = await _lenderRepository.GetAllAsync(cancellationToken);
        var allSchedules = new List<Domain.Entities.RepaymentSchedule>();

        foreach (var lender in lenders)
        {
            var schedules = await _repository.GetSchedulesByLenderIdAsync(
                lender.Id, cancellationToken);
            allSchedules.AddRange(schedules);
        }

        var defaultedSchedules = allSchedules
            .Where(s => s.Performance == LoanPerformance.Defaulted)
            .ToList();

        var items = new List<CollectionItemDto>();

        foreach (var schedule in defaultedSchedules)
        {
            var lender = lenders.FirstOrDefault(l => l.Id == schedule.LenderId);
            var borrowerName = "Unknown";

            if (schedule.LoanApplication is not null)
            {
                var borrower = await _borrowerRepository.GetByIdAsync(
                    schedule.LoanApplication.BorrowerId, cancellationToken);
                if (borrower is not null)
                    borrowerName = $"{borrower.FirstName} {borrower.LastName}";
            }

            var missedCount = schedule.Installments
                .Count(i => i.Status is InstallmentStatus.Late or InstallmentStatus.Missed);

            var outstanding = schedule.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Sum(i => i.TotalAmount + i.LateFeeAmount - i.PaidAmount);

            items.Add(new CollectionItemDto
            {
                ScheduleId = schedule.Id,
                BorrowerName = borrowerName,
                LenderName = lender?.CompanyName ?? "Unknown",
                OutstandingAmount = outstanding,
                MissedInstallments = missedCount,
                DefaultDate = schedule.UpdatedAtUtc ?? schedule.GeneratedAtUtc,
                CollectionStatus = "New"
            });
        }

        return ApiResponse<IReadOnlyList<CollectionItemDto>>.Ok(
            items.OrderByDescending(i => i.DefaultDate).ToList(),
            "Collections retrieved successfully.");
    }
}
