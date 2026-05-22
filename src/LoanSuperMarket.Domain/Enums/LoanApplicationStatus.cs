namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a loan application.
/// </summary>
public enum LoanApplicationStatus
{
    /// <summary>Application created but not yet submitted.</summary>
    Draft = 1,

    /// <summary>Application submitted by the borrower for review.</summary>
    Submitted = 2,

    /// <summary>Application is being reviewed by a CRM manager.</summary>
    UnderReview = 3,

    /// <summary>Application has been approved.</summary>
    Approved = 4,

    /// <summary>Application has been rejected.</summary>
    Rejected = 5,

    /// <summary>Approved application has been funded.</summary>
    Funded = 6,

    /// <summary>Application withdrawn by the borrower.</summary>
    Withdrawn = 7,

    /// <summary>Additional documents have been requested from the borrower.</summary>
    DocumentsRequested = 8
}