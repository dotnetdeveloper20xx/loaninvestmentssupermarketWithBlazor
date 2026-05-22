namespace LoanSuperMarket.Shared.LoanApplications;

public sealed record WizardApplicationSummaryDto(
    Guid ApplicationId,
    string? ProductTitle,
    decimal RequestedAmount,
    int TermMonths,
    DateTime? SubmittedAtUtc,
    int Status,
    int MatchedProductCount,
    int UploadedDocuments,
    int VerifiedDocuments,
    int RejectedDocuments);
