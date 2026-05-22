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

    private LoanApplication(
        Guid borrowerId,
        Money requestedAmount,
        int termMonths,
        string purpose)
    {
        BorrowerId = borrowerId;
        LoanProductId = null;
        RequestedAmount = requestedAmount;
        TermMonths = termMonths;
        Purpose = purpose;
        Status = LoanApplicationStatus.Draft;
        SubmittedAtUtc = null;
    }

    public Guid BorrowerId { get; private set; }

    public Guid? LoanProductId { get; private set; }

    public Money RequestedAmount { get; private set; }

    public int TermMonths { get; private set; }

    public string Purpose { get; private set; }

    public LoanApplicationStatus Status { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? ReviewedBy { get; private set; }

    public string? ReviewReason { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? DocumentRequestNote { get; private set; }

    public ICollection<ApplicationDocument> Documents { get; private set; } = [];

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

    public static LoanApplication CreateDraft(
        Guid borrowerId,
        decimal requestedAmount,
        int termMonths,
        string purpose)
    {
        if (borrowerId == Guid.Empty)
        {
            throw new DomainException("Borrower id is required.");
        }

        if (requestedAmount <= 0)
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
            Money.Create(requestedAmount),
            termMonths,
            purpose.Trim());
    }

    public void UpdateParameters(decimal amount, int termMonths, string purpose)
    {
        if (Status != LoanApplicationStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot update parameters when application is in '{Status}' status. Only draft applications can be updated.");
        }

        if (amount <= 0)
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

        RequestedAmount = Money.Create(amount);
        TermMonths = termMonths;
        Purpose = purpose.Trim();
        MarkUpdated();
    }

    public void SelectProduct(Guid loanProductId)
    {
        if (Status != LoanApplicationStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot select a product when application is in '{Status}' status. Only draft applications can have a product selected.");
        }

        if (loanProductId == Guid.Empty)
        {
            throw new DomainException("Loan product id is required.");
        }

        LoanProductId = loanProductId;
        MarkUpdated();
    }

    public void Submit()
    {
        if (Status != LoanApplicationStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot submit application when it is in '{Status}' status. Only draft applications can be submitted.");
        }

        if (LoanProductId is null || LoanProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Cannot submit application without a selected loan product.");
        }

        Status = LoanApplicationStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkUnderReview()
    {
        if (Status != LoanApplicationStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Cannot move application to under review when it is in '{Status}' status. Only submitted applications can move under review.");
        }

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Approve(string reason, string reviewedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
        {
            throw new InvalidOperationException(
                $"Cannot approve application when it is in '{Status}' status. Only applications under review can be approved.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Approval reason is required.");
        }

        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            throw new DomainException("Reviewer identity is required.");
        }

        Status = LoanApplicationStatus.Approved;
        ReviewReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reject(string reason, string reviewedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
        {
            throw new InvalidOperationException(
                $"Cannot reject application when it is in '{Status}' status. Only applications under review can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Rejection reason is required.");
        }

        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            throw new DomainException("Reviewer identity is required.");
        }

        Status = LoanApplicationStatus.Rejected;
        ReviewReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void RequestDocuments(string note, string requestedBy)
    {
        if (Status != LoanApplicationStatus.UnderReview)
        {
            throw new InvalidOperationException(
                $"Cannot request documents when application is in '{Status}' status. Only applications under review can have documents requested.");
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException("Document request note is required.");
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new DomainException("Requester identity is required.");
        }

        Status = LoanApplicationStatus.DocumentsRequested;
        DocumentRequestNote = note;
        ReviewedBy = requestedBy;
        MarkUpdated();
    }

    public void ResubmitForReview()
    {
        if (Status != LoanApplicationStatus.DocumentsRequested)
        {
            throw new InvalidOperationException(
                $"Cannot resubmit for review when application is in '{Status}' status. Only applications with documents requested can be resubmitted.");
        }

        Status = LoanApplicationStatus.UnderReview;
        MarkUpdated();
    }

    public void Fund()
    {
        if (Status != LoanApplicationStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot fund application when it is in '{Status}' status. Only approved applications can be funded.");
        }

        Status = LoanApplicationStatus.Funded;
        MarkUpdated();
    }

    public void Withdraw()
    {
        if (Status != LoanApplicationStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot withdraw application when it is in '{Status}' status. Only draft applications can be withdrawn.");
        }

        Status = LoanApplicationStatus.Withdrawn;
        MarkUpdated();
    }
}
