namespace LoanSuperMarket.Shared.LoanApplications;

public sealed record MatchedProductDto(
    Guid ProductId,
    string Title,
    string LenderName,
    decimal EffectiveInterestRate,
    decimal MinimumAmount,
    decimal MaximumAmount,
    int MinimumTermMonths,
    int MaximumTermMonths);
