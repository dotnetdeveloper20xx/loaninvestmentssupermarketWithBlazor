namespace LoanSuperMarket.Shared.Grids;

public sealed class GridQueryRequest
{
    public string? SearchText { get; set; }

    public string? Status { get; set; }

    public string SortColumn { get; set; } = "CreatedAtUtc";

    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}