namespace LoanSuperMarket.Blazor.Services.Notifications;

public sealed class ToastService
{
    private readonly List<ToastMessage> _messages = [];

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? OnChange;

    public void ShowSuccess(string title, string message)
    {
        Add(ToastLevel.Success, title, message);
    }

    public void ShowError(string title, string message)
    {
        Add(ToastLevel.Error, title, message);
    }

    public void ShowInfo(string title, string message)
    {
        Add(ToastLevel.Info, title, message);
    }

    public void ShowWarning(string title, string message)
    {
        Add(ToastLevel.Warning, title, message);
    }

    public void Remove(Guid id)
    {
        var message = _messages.FirstOrDefault(x => x.Id == id);

        if (message is null)
        {
            return;
        }

        _messages.Remove(message);
        NotifyStateChanged();
    }

    private void Add(ToastLevel level, string title, string message)
    {
        var toast = new ToastMessage
        {
            Level = level,
            Title = title,
            Message = message
        };

        _messages.Add(toast);
        NotifyStateChanged();

        _ = AutoCloseAsync(toast.Id);
    }

    private async Task AutoCloseAsync(Guid id)
    {
        await Task.Delay(4000);

        var message = _messages.FirstOrDefault(x => x.Id == id);

        if (message is null)
        {
            return;
        }

        message.IsClosing = true;
        NotifyStateChanged();

        await Task.Delay(300);

        Remove(id);
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}