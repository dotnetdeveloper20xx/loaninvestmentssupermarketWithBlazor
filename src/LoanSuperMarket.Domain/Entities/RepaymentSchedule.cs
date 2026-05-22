using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class RepaymentSchedule : AuditableEntity
{
    private readonly List<Installment> _installments = [];

    private RepaymentSchedule() { }

    public RepaymentSchedule(
        Guid loanApplicationId,
        Guid lenderId,
        decimal fundedAmount,
        decimal annualInterestRate,
        int termMonths,
        decimal monthlyEmi,
        decimal totalInterestPayable)
    {
        LoanApplicationId = loanApplicationId;
        LenderId = lenderId;
        FundedAmount = fundedAmount;
        AnnualInterestRate = annualInterestRate;
        TermMonths = termMonths;
        MonthlyEmi = monthlyEmi;
        TotalInterestPayable = totalInterestPayable;
        Performance = LoanPerformance.OnTime;
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public Guid LoanApplicationId { get; private set; }

    public Guid LenderId { get; private set; }

    public decimal FundedAmount { get; private set; }

    public decimal AnnualInterestRate { get; private set; }

    public int TermMonths { get; private set; }

    public decimal MonthlyEmi { get; private set; }

    public decimal TotalInterestPayable { get; private set; }

    public LoanPerformance Performance { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    // Navigation properties
    public LoanApplication? LoanApplication { get; private set; }
    public Lender? Lender { get; private set; }

    public void AddInstallment(Installment installment)
    {
        _installments.Add(installment);
    }

    public Installment? GetNextPendingInstallment()
    {
        return _installments
            .Where(i => i.Status is InstallmentStatus.Pending
                or InstallmentStatus.PartiallyPaid
                or InstallmentStatus.Late
                or InstallmentStatus.Missed)
            .OrderBy(i => i.InstallmentNumber)
            .FirstOrDefault();
    }

    public decimal GetTotalPaidToDate()
    {
        return _installments.Sum(i => i.PaidAmount);
    }

    public void UpdatePerformance()
    {
        var orderedInstallments = _installments
            .OrderByDescending(i => i.InstallmentNumber)
            .ToList();

        var consecutiveBad = 0;
        foreach (var installment in orderedInstallments)
        {
            if (installment.Status is InstallmentStatus.Late or InstallmentStatus.Missed)
            {
                consecutiveBad++;
            }
            else
            {
                break;
            }
        }

        if (consecutiveBad >= 3)
        {
            Performance = LoanPerformance.Defaulted;
        }
        else if (_installments.Any(i => i.Status is InstallmentStatus.Late or InstallmentStatus.Missed))
        {
            Performance = LoanPerformance.Late;
        }
        else
        {
            Performance = LoanPerformance.OnTime;
        }

        MarkUpdated();
    }
}
