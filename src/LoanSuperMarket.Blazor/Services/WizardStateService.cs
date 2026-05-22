namespace LoanSuperMarket.Blazor.Services;

public sealed class WizardStateService
{
    public Guid? ApplicationId { get; private set; }

    public int CurrentStep { get; private set; } = 1;

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
    }
}
