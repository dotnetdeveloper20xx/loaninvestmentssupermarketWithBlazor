using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Features.Funding;

/// <summary>
/// Generates amortization schedules using the standard EMI formula:
/// EMI = P × r × (1+r)^n / ((1+r)^n - 1)
/// where r = annualRate / 12 / 100, n = termMonths.
/// </summary>
public sealed class AmortizationService : IAmortizationService
{
    public RepaymentSchedule GenerateSchedule(
        Guid loanApplicationId,
        Guid lenderId,
        decimal principal,
        decimal annualInterestRate,
        int termMonths,
        DateTime fundingDate)
    {
        if (principal <= 0)
        {
            throw new DomainException("Principal amount must be greater than zero.");
        }

        if (annualInterestRate <= 0)
        {
            throw new DomainException("Annual interest rate must be greater than zero.");
        }

        if (termMonths <= 0)
        {
            throw new DomainException("Term months must be greater than zero.");
        }

        var monthlyRate = annualInterestRate / 12m / 100m;

        // EMI = P × r × (1+r)^n / ((1+r)^n - 1)
        var compoundFactor = (decimal)Math.Pow((double)(1m + monthlyRate), termMonths);
        var emi = principal * monthlyRate * compoundFactor / (compoundFactor - 1m);
        emi = decimal.Round(emi, 2);

        var totalInterest = (emi * termMonths) - principal;

        var schedule = new RepaymentSchedule(
            loanApplicationId,
            lenderId,
            principal,
            annualInterestRate,
            termMonths,
            emi,
            decimal.Round(totalInterest, 2));

        var remainingBalance = principal;
        var totalPrincipalAllocated = 0m;

        for (var i = 1; i <= termMonths; i++)
        {
            var interestPortion = decimal.Round(remainingBalance * monthlyRate, 2);
            decimal principalPortion;

            if (i == termMonths)
            {
                // Final installment absorbs rounding difference
                principalPortion = remainingBalance;
            }
            else
            {
                principalPortion = decimal.Round(emi - interestPortion, 2);
            }

            remainingBalance -= principalPortion;
            totalPrincipalAllocated += principalPortion;

            var dueDate = fundingDate.AddMonths(i);

            var installment = new Installment(
                installmentNumber: i,
                dueDate: dueDate,
                principalPortion: principalPortion,
                interestPortion: interestPortion,
                remainingBalance: Math.Max(remainingBalance, 0));

            schedule.AddInstallment(installment);
        }

        return schedule;
    }
}
