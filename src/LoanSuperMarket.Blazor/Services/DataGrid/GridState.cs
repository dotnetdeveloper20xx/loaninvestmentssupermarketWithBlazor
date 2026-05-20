namespace LoanSuperMarket.Blazor.Services.DataGrid;

public sealed class GridState
{
    public string SearchText { get; set; } = string.Empty;

    public string SelectedStatus { get; set; } = string.Empty;

    public string SortColumn { get; set; } = "CreatedAtUtc";

    public bool SortAscending { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedStatus = string.Empty;
        PageNumber = 1;
    }

    public void SortBy(string column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = true;
        }

        PageNumber = 1;
    }

    public string GetSortIcon(string column)
    {
        if (SortColumn != column)
        {
            return "↕";
        }

        return SortAscending ? "↑" : "↓";
    }
}