namespace LoanSuperMarket.Blazor.Services.Modals;

public sealed class ConfirmationModalRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ConfirmText { get; init; } = "Confirm";

    public string CancelText { get; init; } = "Cancel";

    public ModalIntent Intent { get; init; } = ModalIntent.Info;

    public Func<Task>? OnConfirmAsync { get; init; }

    public bool IsProcessing { get; set; }
}