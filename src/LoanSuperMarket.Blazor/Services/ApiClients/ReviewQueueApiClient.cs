using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class ReviewQueueApiClient
{
    private readonly HttpClient _httpClient;

    public ReviewQueueApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>?> GetQueueAsync(
        int? statusFilter = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = "api/review-queue";
        var queryParams = new List<string>();

        if (statusFilter.HasValue)
            queryParams.Add($"status={statusFilter.Value}");
        if (!string.IsNullOrEmpty(sortBy))
            queryParams.Add($"sortBy={sortBy}");

        if (queryParams.Count > 0)
            query += "?" + string.Join("&", queryParams);

        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>>(
            query,
            cancellationToken);
    }

    public async Task<ApiResponse<ApplicationDetailDto>?> GetDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<ApplicationDetailDto>>(
            $"api/review-queue/{applicationId}",
            cancellationToken);
    }

    public async Task<ApiResponse<string>?> MarkUnderReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/review-queue/{applicationId}/mark-under-review",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> ApproveAsync(
        Guid applicationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = new ApproveRejectRequest { Reason = reason };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/approve",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> RejectAsync(
        Guid applicationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = new ApproveRejectRequest { Reason = reason };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/reject",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> RequestDocumentsAsync(
        Guid applicationId,
        string note,
        CancellationToken cancellationToken = default)
    {
        var request = new RequestDocumentsRequest { Note = note };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/request-documents",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> VerifyDocumentAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/review-queue/{applicationId}/documents/{documentId}/verify",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    public async Task<ApiResponse<string>?> RejectDocumentAsync(
        Guid applicationId,
        Guid documentId,
        string rejectionNote,
        CancellationToken cancellationToken = default)
    {
        var request = new RejectDocumentRequest { RejectionNote = rejectionNote };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/documents/{documentId}/reject",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }
}
