using Microsoft.AspNetCore.Components;

namespace LoanSuperMarket.Blazor.Services.Drawers;

public sealed class DrawerRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public RenderFragment? Content { get; init; }

    public bool IsClosing { get; set; }
}