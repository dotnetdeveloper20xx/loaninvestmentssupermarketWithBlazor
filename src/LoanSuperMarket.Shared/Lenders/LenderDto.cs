namespace LoanSuperMarket.Shared.Lenders;

public sealed class LenderDto
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public decimal AvailableFunds { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}