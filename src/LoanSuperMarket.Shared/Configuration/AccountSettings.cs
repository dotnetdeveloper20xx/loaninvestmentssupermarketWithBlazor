namespace LoanSuperMarket.Shared.Configuration;

/// <summary>
/// Configuration model for account security and operational limits.
/// Bound from the "AccountSettings" section in appsettings.json.
/// </summary>
public sealed class AccountSettings
{
    public const string SectionName = "AccountSettings";

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;

    public int MaxActiveLoansPerBorrower { get; set; } = 5;

    public int SessionInactivityTimeoutMinutes { get; set; } = 30;
}
