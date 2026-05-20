using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;

namespace LoanSuperMarket.Domain.Entities;

public sealed class LoanApplication : AuditableEntity
{
    private LoanApplication()
    {
        Purpose = string.Empty;
        RequestedAmount = Money.Create(1);
    }

    private LoanApplication(
        Guid borrowerId,
        Guid loanProductId,
        Money requestedAmount,
        int termMonths,
        string purpose)
    {
        BorrowerId = borrowerId;
        LoanProductId = loanProductId;
        RequestedAmount = requestedAmount;
        TermMonths = termMonths;
        Purpose = purpose;
        Status = LoanApplicationStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public Guid BorrowerId { get; private set; }

    public Guid LoanProductId { get; private set; }

    public Money RequestedAmount { get; private set; }

    public int TermMonths { get; private set; }

    public string Purpose { get; private set; }

    public LoanApplicationStatus Status { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public static LoanApplication Create(
        Guid borrowerId,
        Guid loanProductId,
        Money requestedAmount,
        int termMonths,
        string purpose)
    {
        if (borrowerId == Guid.Empty)
        {
            throw new DomainException("Borrower id is required.");
        }

        if (loanProductId == Guid.Empty)
        {
            throw new DomainException("Loan product id is required.");
        }

        if (requestedAmount.Amount <= 0)
        {
            throw new DomainException("Requested amount must be greater than zero.");
        }

        if (termMonths <= 0)
        {
            throw new DomainException("Term must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainException("Loan purpose is required.");
        }

        if (purpose.Length > 1000)
        {
            throw new DomainException("Loan purpose cannot exceed 1000 characters.");
        }

        return new LoanApplication(
            borrowerId,
            loanProductId,
            requestedAmount,
            termMonths,
            purpose.Trim());
    }

    public void MarkUnderReview()
    {
        if (Status != LoanApplicationStatus.Submitted)
        {
            throw new DomainException("Only submitted applications can move under review.");
        }

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != LoanApplicationStatus.UnderReview)
        {
            throw new DomainException("Only applications under review can be approved.");
        }

        Status = LoanApplicationStatus.Approved;
        MarkUpdated();
    }

    public void Reject()
    {
        if (Status is LoanApplicationStatus.Approved or LoanApplicationStatus.Funded)
        {
            throw new DomainException("Approved or funded applications cannot be rejected.");
        }

        Status = LoanApplicationStatus.Rejected;
        MarkUpdated();
    }

    public void Fund()
    {
        if (Status != LoanApplicationStatus.Approved)
        {
            throw new DomainException("Only approved applications can be funded.");
        }

        Status = LoanApplicationStatus.Funded;
        MarkUpdated();
    }

    public void Withdraw()
    {
        if (Status == LoanApplicationStatus.Funded)
        {
            throw new DomainException("Funded applications cannot be withdrawn.");
        }

        Status = LoanApplicationStatus.Withdrawn;
        MarkUpdated();
    }
}