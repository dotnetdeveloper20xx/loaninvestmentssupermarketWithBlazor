using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.FundLoan;

public sealed class FundLoanCommandHandler
    : IRequestHandler<FundLoanCommand, ApiResponse<FundingResultDto>>
{
    private readonly ILenderRepository _lenderRepository;
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly ILoanProductRepository _loanProductRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly IAmortizationService _amortizationService;
    private readonly IAuditLogRepository _auditLogRepository;

    public FundLoanCommandHandler(
        ILenderRepository lenderRepository,
        ILoanApplicationRepository loanApplicationRepository,
        ILoanProductRepository loanProductRepository,
        IBorrowerRepository borrowerRepository,
        IAmortizationService amortizationService,
        IAuditLogRepository auditLogRepository)
    {
        _lenderRepository = lenderRepository;
        _loanApplicationRepository = loanApplicationRepository;
        _loanProductRepository = loanProductRepository;
        _borrowerRepository = borrowerRepository;
        _amortizationService = amortizationService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<FundingResultDto>> Handle(
        FundLoanCommand request,
        CancellationToken cancellationToken)
    {
        var lender = await _lenderRepository.GetByIdAsync(request.LenderId, cancellationToken);
        if (lender is null)
        {
            throw new DomainException("Lender not found.");
        }

        var application = await _loanApplicationRepository.GetByIdAsync(
            request.ApplicationId, cancellationToken);
        if (application is null)
        {
            throw new DomainException("Loan application not found.");
        }

        if (application.LoanProductId is null)
        {
            throw new DomainException("Loan application does not have a product selected.");
        }

        var product = await _loanProductRepository.GetByIdAsync(
            application.LoanProductId.Value, cancellationToken);
        if (product is null)
        {
            throw new DomainException("Loan product not found.");
        }

        // Get borrower for credit tier
        var borrower = await _borrowerRepository.GetByIdAsync(
            application.BorrowerId, cancellationToken);

        // Calculate effective rate: base rate + credit tier adjustment
        var baseRate = product.InterestRate.Percentage;
        var effectiveRate = CalculateEffectiveRate(baseRate, borrower?.CreditTier);

        var fundingAmount = application.RequestedAmount.Amount;

        // Deduct funds from lender
        lender.DeductFunds(fundingAmount);

        // Mark application as funded
        application.Fund();

        // Generate amortization schedule
        var schedule = _amortizationService.GenerateSchedule(
            application.Id,
            lender.Id,
            fundingAmount,
            effectiveRate,
            application.TermMonths,
            DateTime.UtcNow);

        // Persist schedule
        await _loanApplicationRepository.AddRepaymentScheduleAsync(schedule, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "Funded",
                $"Loan funded by lender {lender.CompanyName}. EMI: {schedule.MonthlyEmi:N2}, Term: {schedule.TermMonths} months."),
            cancellationToken);

        await _lenderRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<FundingResultDto>.Ok(new FundingResultDto
        {
            ScheduleId = schedule.Id,
            MonthlyEmi = schedule.MonthlyEmi,
            TotalInterest = schedule.TotalInterestPayable,
            TermMonths = schedule.TermMonths,
            FundedAmount = schedule.FundedAmount,
            EffectiveRate = effectiveRate
        }, "Loan funded successfully.");
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
