namespace LoanSuperMarket.Blazor.Components.Common;

public sealed class BarChartItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Color { get; set; }
}
