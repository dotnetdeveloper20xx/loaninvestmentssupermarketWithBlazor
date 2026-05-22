namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Represents the type of document uploaded for a loan application.
/// </summary>
public enum DocumentType
{
    /// <summary>Government-issued national identification document.</summary>
    NationalID = 1,

    /// <summary>Proof of income such as payslips or tax returns.</summary>
    ProofOfIncome = 2,

    /// <summary>Recent bank statement showing financial activity.</summary>
    BankStatement = 3,

    /// <summary>Proof of residential address such as a utility bill.</summary>
    AddressProof = 4,

    /// <summary>Any other supporting document.</summary>
    Other = 5
}
