using System.Net.Http.Json;
using LoanSuperMarket.Shared.Borrowers;
using LoanSuperMarket.Shared.Common;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class BorrowersApiClient
{
    private readonly HttpClient _httpClient;

    public BorrowersApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<BorrowerDto>>?> GetBorrowersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<BorrowerDto>>>(
            "api/borrowers",
            cancellationToken);
    }

    public async Task<ApiResponse<Guid>?> CreateBorrowerAsync(
        CreateBorrowerRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/borrowers",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            cancellationToken);
    }
}