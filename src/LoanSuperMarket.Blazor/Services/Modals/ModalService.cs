namespace LoanSuperMarket.Blazor.Services.Modals;

public sealed class ModalService
{
    public ConfirmationModalRequest? CurrentConfirmation { get; private set; }

    public event Action? OnChange;

    public void ShowConfirmation(ConfirmationModalRequest request)
    {
        CurrentConfirmation = request;
        NotifyStateChanged();
    }

    public void Close()
    {
        CurrentConfirmation = null;
        NotifyStateChanged();
    }

    public async Task ConfirmAsync()
    {
        if (CurrentConfirmation is null)
        {
            return;
        }

        if (CurrentConfirmation.OnConfirmAsync is null)
        {
            Close();
            return;
        }

        CurrentConfirmation.IsProcessing = true;
        NotifyStateChanged();

        try
        {
            await CurrentConfirmation.OnConfirmAsync();
            Close();
        }
        finally
        {
            if (CurrentConfirmation is not null)
            {
                CurrentConfirmation.IsProcessing = false;
                NotifyStateChanged();
            }
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}