namespace LoanSuperMarket.Shared.Configuration;

public sealed class NotificationPreferencesDto
{
    public bool PaymentReminders { get; set; } = true;

    public bool LatePaymentNotices { get; set; } = true;

    public bool DefaultNotices { get; set; } = true;

    public bool FundingConfirmations { get; set; } = true;

    public bool PortfolioSummary { get; set; } = false;

    public string PreferredChannel { get; set; } = "Email";
}
