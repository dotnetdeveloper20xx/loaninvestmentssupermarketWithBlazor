using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Lenders;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class LendersApiClient
{
    private readonly HttpClient _httpClient;

    public LendersApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<LenderDto>>?> GetLendersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<LenderDto>>>(
            "api/lenders",
            cancellationToken);
    }

    public async Task<ApiResponse<Guid>?> CreateLenderAsync(
        CreateLenderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/lenders",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            cancellationToken);
    }
}