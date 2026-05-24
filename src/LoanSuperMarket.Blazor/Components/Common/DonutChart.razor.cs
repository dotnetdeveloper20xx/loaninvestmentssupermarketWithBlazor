namespace LoanSuperMarket.Blazor.Components.Common;

public sealed class DonutSegment
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = "bg-blue-500";
    public string? CssColor { get; set; } = "#3b82f6";
}
