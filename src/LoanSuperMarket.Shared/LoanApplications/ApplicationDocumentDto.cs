namespace LoanSuperMarket.Shared.LoanApplications;

public sealed record ApplicationDocumentDto(
    Guid Id,
    string FileName,
    int Type,
    int Status,
    DateTime UploadedAtUtc,
    string? VerifiedBy,
    DateTime? VerifiedAtUtc,
    string? RejectionNote);
