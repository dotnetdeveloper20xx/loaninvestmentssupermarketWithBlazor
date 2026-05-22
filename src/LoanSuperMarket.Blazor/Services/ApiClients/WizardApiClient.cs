using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class WizardApiClient
{
    private readonly HttpClient _httpClient;

    public WizardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<Guid>?> CreateDraftAsync(
        CreateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/wizard/create-draft",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> UpdateParametersAsync(
        Guid applicationId,
        UpdateDraftParametersRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/wizard/{applicationId}/parameters",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<MatchedProductDto>>?> MatchProductsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/match-products",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<MatchedProductDto>>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> SelectProductAsync(
        Guid applicationId,
        SelectProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/wizard/{applicationId}/select-product",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<Guid>?> UploadDocumentAsync(
        Guid applicationId,
        Stream fileStream,
        string fileName,
        int documentType,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(documentType.ToString()), "documentType");

        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/documents",
            content,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> RemoveDocumentAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/wizard/{applicationId}/documents/{documentId}",
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<ApplicationDocumentDto>>?> GetDocumentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ApplicationDocumentDto>>>(
            $"api/wizard/{applicationId}/documents",
            cancellationToken);
    }

    public async Task<ApiResponse<string>?> SubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/submit",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> WithdrawAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/withdraw",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> ResubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/resubmit",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>?> GetBorrowerApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>>(
            "api/borrower/applications",
            cancellationToken);
    }
}
