namespace LoanSuperMarket.Blazor.Services;

public sealed class WizardStateService
{
    public Guid? ApplicationId { get; private set; }

    public int CurrentStep { get; private set; } = 1;

    // Data from Step 1
    public string Purpose { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }

    // Data from Step 3
    public string? SelectedProductTitle { get; set; }

    public void SetApplicationId(Guid id)
    {
        ApplicationId = id;
    }

    public void GoToStep(int step)
    {
        if (step < 1 || step > 5)
            return;

        CurrentStep = step;
    }

    public void Reset()
    {
        ApplicationId = null;
        CurrentStep = 1;
        Purpose = string.Empty;
        RequestedAmount = 0;
        TermMonths = 0;
        SelectedProductTitle = null;
    }
}
