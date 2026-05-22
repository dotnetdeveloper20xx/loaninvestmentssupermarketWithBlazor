using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.GetFundingApplicationDetails;

public sealed class GetFundingApplicationDetailsQueryHandler
    : IRequestHandler<GetFundingApplicationDetailsQuery, ApiResponse<FundingApplicationDetailDto>>
{
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public GetFundingApplicationDetailsQueryHandler(
        ILoanApplicationRepository loanApplicationRepository,
        ILoanProductRepository loanProductRepository,
        IBorrowerRepository borrowerRepository)
    {
        _loanApplicationRepository = loanApplicationRepository;
        _loanProductRepository = loanProductRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ApiResponse<FundingApplicationDetailDto>> Handle(
        GetFundingApplicationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var application = await _loanApplicationRepository.GetByIdAsync(
            request.ApplicationId, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application not found.");
        }

        var borrower = await _borrowerRepository.GetByIdAsync(
            application.BorrowerId, cancellationToken);

        var product = application.LoanProductId.HasValue
            ? await _loanProductRepository.GetByIdAsync(application.LoanProductId.Value, cancellationToken)
            : null;

        var baseRate = product?.InterestRate.Percentage ?? 0;
        var effectiveRate = CalculateEffectiveRate(baseRate, borrower?.CreditTier);

        var dto = new FundingApplicationDetailDto
        {
            ApplicationId = application.Id,
            BorrowerName = borrower is not null
                ? $"{borrower.FirstName} {borrower.LastName}"
                : "Unknown",
            BorrowerEmail = borrower?.Email ?? string.Empty,
            CreditTier = borrower?.CreditTier.ToString() ?? "Unknown",
            Amount = application.RequestedAmount.Amount,
            TermMonths = application.TermMonths,
            ProductTitle = product?.Title ?? "Unknown",
            BaseRate = baseRate,
            EffectiveRate = effectiveRate,
            Purpose = application.Purpose,
            ApprovalReason = application.ReviewReason,
            ApprovalDate = application.ReviewedAtUtc ?? application.CreatedAtUtc
        };

        return ApiResponse<FundingApplicationDetailDto>.Ok(
            dto,
            "Application details retrieved successfully.");
    }

    private static decimal CalculateEffectiveRate(decimal baseRate, CreditTier? creditTier)
    {
        return creditTier switch
        {
            CreditTier.A => baseRate,
            CreditTier.B => baseRate + 2m,
            CreditTier.C => baseRate + 4m,
            _ => baseRate
        };
    }
}
