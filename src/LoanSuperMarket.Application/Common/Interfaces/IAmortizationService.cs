using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for generating amortization schedules using the EMI formula.
/// </summary>
public interface IAmortizationService
{
    /// <summary>
    /// Generates a complete repayment schedule with installments.
    /// </summary>
    RepaymentSchedule GenerateSchedule(
        Guid loanApplicationId,
        Guid lenderId,
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateTime fundingDate);
}
