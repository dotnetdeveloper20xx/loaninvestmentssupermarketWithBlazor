namespace LoanSuperMarket.Shared.LoanApplications;

public sealed record ReviewQueueItemDto(
    Guid ApplicationId,
    string BorrowerName,
    decimal RequestedAmount,
    string ProductTitle,
    DateTime SubmittedAtUtc,
    int Status,
    int DocumentCount,
    int VerifiedDocumentCount);
