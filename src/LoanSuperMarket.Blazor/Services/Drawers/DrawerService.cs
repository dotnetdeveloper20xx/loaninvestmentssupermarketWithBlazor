namespace LoanSuperMarket.Blazor.Services.Drawers;

public sealed class DrawerService
{
    public DrawerRequest? CurrentDrawer { get; private set; }

    public event Action? OnChange;

    public void Open(DrawerRequest request)
    {
        CurrentDrawer = request;
        NotifyStateChanged();
    }

    public async Task CloseAsync()
    {
        if (CurrentDrawer is null)
        {
            return;
        }

        CurrentDrawer.IsClosing = true;
        NotifyStateChanged();

        await Task.Delay(250);

        CurrentDrawer = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}