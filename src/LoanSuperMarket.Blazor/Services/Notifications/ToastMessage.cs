namespace LoanSuperMarket.Blazor.Services.Notifications;

public sealed class ToastMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ToastLevel Level { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public bool IsClosing { get; set; }
}