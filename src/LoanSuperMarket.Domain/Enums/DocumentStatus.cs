namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Represents the verification status of an uploaded document.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Document uploaded but not yet reviewed.</summary>
    Pending = 1,

    /// <summary>Document has been verified by a CRM manager.</summary>
    Verified = 2,

    /// <summary>Document has been rejected by a CRM manager.</summary>
    Rejected = 3
}
