using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class ApplicationDocument : AuditableEntity
{
    private ApplicationDocument()
    {
        FileName = string.Empty;
        StorageReference = string.Empty;
    }

    private ApplicationDocument(
        Guid loanApplicationId,
        DocumentType type,
        string fileName,
        string storageReference)
    {
        LoanApplicationId = loanApplicationId;
        Type = type;
        FileName = fileName;
        StorageReference = storageReference;
        UploadedAtUtc = DateTime.UtcNow;
        Status = DocumentStatus.Pending;
    }

    public Guid LoanApplicationId { get; private set; }

    public DocumentType Type { get; private set; }

    public string FileName { get; private set; }

    public string StorageReference { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    public DocumentStatus Status { get; private set; }

    public string? VerifiedBy { get; private set; }

    public DateTime? VerifiedAtUtc { get; private set; }

    public string? RejectionNote { get; private set; }

    public LoanApplication LoanApplication { get; private set; } = null!;

    public static ApplicationDocument Create(
        Guid loanApplicationId,
        DocumentType type,
        string fileName,
        string storageReference)
    {
        if (loanApplicationId == Guid.Empty)
        {
            throw new DomainException("Loan application id is required.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(storageReference))
        {
            throw new DomainException("Storage reference is required.");
        }

        return new ApplicationDocument(loanApplicationId, type, fileName, storageReference);
    }

    public void Verify(string verifiedBy)
    {
        if (Status != DocumentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot verify document when it is in '{Status}' status. Only pending documents can be verified.");
        }

        if (string.IsNullOrWhiteSpace(verifiedBy))
        {
            throw new DomainException("Verifier identity is required.");
        }

        Status = DocumentStatus.Verified;
        VerifiedBy = verifiedBy;
        VerifiedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reject(string rejectedBy, string rejectionNote)
    {
        if (Status != DocumentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot reject document when it is in '{Status}' status. Only pending documents can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(rejectedBy))
        {
            throw new DomainException("Rejector identity is required.");
        }

        if (string.IsNullOrWhiteSpace(rejectionNote))
        {
            throw new DomainException("Rejection note is required.");
        }

        Status = DocumentStatus.Rejected;
        VerifiedBy = rejectedBy;
        VerifiedAtUtc = DateTime.UtcNow;
        RejectionNote = rejectionNote;
        MarkUpdated();
    }
}
