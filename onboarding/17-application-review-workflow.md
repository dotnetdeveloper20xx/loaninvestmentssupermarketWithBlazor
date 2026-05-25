# 17 — Application Review Workflow

## Feature Requirements

The review workflow allows admins/CRM managers to process submitted loan applications. Key requirements:

1. **Review Queue**: List all submitted/under-review/documents-requested applications
2. **Filtering**: Filter by status, sort by date/amount/status
3. **Application Detail**: View full application details including documents
4. **Actions**: Mark under review, approve, reject, request additional documents
5. **Document Verification**: Verify or reject individual uploaded documents
6. **Status Flow**: Submitted → UnderReview → Approved/Rejected/DocumentsRequested

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| CQRS | `GetReviewQueueQuery` for reads, commands for writes |
| MediatR | Decouples controller from business logic |
| Authorization Policy | `CanProcessApplications` restricts access |
| Blazor Components | `ReviewQueue.razor` and `ReviewApplicationDetail.razor` |

---

## API Layer: `ReviewQueueController.cs`

```csharp
[ApiController]
[Route("api/review-queue")]
[Authorize(Policy = "CanProcessApplications")]
public sealed class ReviewQueueController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewQueueController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>>> GetReviewQueue(
        [FromQuery] int? statusFilter,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var query = new GetReviewQueueQuery(statusFilter, sortBy);
        var items = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReviewQueueItemDto>>.Ok(
            items, "Review queue retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> GetApplicationDetails(
        Guid id, CancellationToken cancellationToken)
    {
        var query = new GetApplicationDetailsQuery(id);
        var details = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<ApplicationDetailDto>.Ok(
            details, "Application details retrieved successfully."));
    }

    [HttpPost("{id:guid}/mark-under-review")]
    public async Task<ActionResult<ApiResponse<string>>> MarkUnderReview(
        Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkLoanApplicationUnderReviewCommand(id), cancellationToken);
        return Ok(ApiResponse<string>.Ok("Application moved under review.", "Workflow action completed."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
        Guid id, [FromBody] ApproveRejectRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ApproveLoanApplicationCommand(id, request.Reason), cancellationToken);
        return Ok(ApiResponse<string>.Ok("Application approved.", "Workflow action completed."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        Guid id, [FromBody] ApproveRejectRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectLoanApplicationCommand(id, request.Reason), cancellationToken);
        return Ok(ApiResponse<string>.Ok("Application rejected.", "Workflow action completed."));
    }

    [HttpPost("{id:guid}/request-documents")]
    public async Task<ActionResult<ApiResponse<string>>> RequestDocuments(
        Guid id, [FromBody] RequestDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestAdditionalDocumentsCommand(id, request.Note);
        await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Additional documents requested.", "Workflow action completed."));
    }

    [HttpPost("{id:guid}/documents/{docId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<string>>> VerifyDocument(
        Guid id, Guid docId, CancellationToken cancellationToken)
    {
        var command = new VerifyDocumentCommand(id, docId);
        await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Document verified.", "Document verification completed."));
    }

    [HttpPost("{id:guid}/documents/{docId:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> RejectDocument(
        Guid id, Guid docId, [FromBody] RejectDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectDocumentCommand(id, docId, request.RejectionNote);
        await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Document rejected.", "Document rejection completed."));
    }
}
```

### Endpoint Summary

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/review-queue` | Get queue with optional filters |
| GET | `/api/review-queue/{id}` | Get application details |
| POST | `/api/review-queue/{id}/mark-under-review` | Transition to UnderReview |
| POST | `/api/review-queue/{id}/approve` | Approve application |
| POST | `/api/review-queue/{id}/reject` | Reject application |
| POST | `/api/review-queue/{id}/request-documents` | Request more documents |
| POST | `/api/review-queue/{id}/documents/{docId}/verify` | Verify a document |
| POST | `/api/review-queue/{id}/documents/{docId}/reject` | Reject a document |

---

## Query: `GetReviewQueueQuery`

The query supports filtering by status and sorting:

```csharp
public sealed record GetReviewQueueQuery(int? StatusFilter, string? SortBy)
    : IRequest<IReadOnlyList<ReviewQueueItemDto>>;
```

### Repository Implementation

```csharp
public async Task<IReadOnlyList<ReviewQueueItemDto>> GetReviewQueueAsync(
    LoanApplicationStatus[]? statusFilter, string? sortBy, CancellationToken ct)
{
    var query = _context.LoanApplications
        .Include(x => x.Documents)
        .AsQueryable();

    if (statusFilter is { Length: > 0 })
        query = query.Where(x => statusFilter.Contains(x.Status));

    query = sortBy?.ToLowerInvariant() switch
    {
        "amount" => query.OrderByDescending(x => x.RequestedAmount.Amount),
        "status" => query.OrderBy(x => x.Status),
        _ => query.OrderByDescending(x => x.SubmittedAtUtc)
    };

    return await query.Select(x => new ReviewQueueItemDto(
        x.Id,
        /* borrower name lookup */,
        x.RequestedAmount.Amount,
        /* product title lookup */,
        x.SubmittedAtUtc ?? x.CreatedAtUtc,
        (int)x.Status,
        x.Documents.Count,
        x.Documents.Count(d => d.Status == DocumentStatus.Verified)
    )).ToListAsync(ct);
}
```

---

## Blazor Frontend: `ReviewQueue.razor`

```razor
@page "/review-queue"
@attribute [Authorize(Roles = "Admin,CrmManager")]
@inject ReviewQueueApiClient ReviewQueueApi
@inject NavigationManager NavigationManager

<div class="max-w-6xl mx-auto">
    <h1 class="text-2xl font-bold">Review Queue</h1>

    <!-- Status filter dropdown -->
    <select @bind="_statusFilter" @bind:after="LoadQueueAsync">
        <option value="">All Statuses</option>
        <option value="1">Submitted</option>
        <option value="2">Under Review</option>
        <option value="8">Documents Requested</option>
    </select>

    <!-- Sort dropdown -->
    <select @bind="_sortBy" @bind:after="LoadQueueAsync">
        <option value="">Sort by Date</option>
        <option value="amount">Sort by Amount</option>
        <option value="status">Sort by Status</option>
    </select>

    <!-- Queue table -->
    <table>
        @foreach (var item in _items)
        {
            <tr @onclick="() => NavigateToDetail(item.ApplicationId)">
                <td>@item.BorrowerName</td>
                <td>@item.ProductTitle</td>
                <td>@item.RequestedAmount.ToString("N2")</td>
                <td>@item.SubmittedAtUtc.ToString("MMM dd, yyyy")</td>
                <td>@GetStatusLabel(item.Status)</td>
                <td>@item.VerifiedDocumentCount / @item.DocumentCount</td>
            </tr>
        }
    </table>
</div>

@code {
    private IReadOnlyList<ReviewQueueItemDto>? _items;
    private string _statusFilter = string.Empty;
    private string _sortBy = string.Empty;

    private async Task LoadQueueAsync()
    {
        int? statusFilter = string.IsNullOrEmpty(_statusFilter) ? null : int.Parse(_statusFilter);
        string? sortBy = string.IsNullOrEmpty(_sortBy) ? null : _sortBy;
        var result = await ReviewQueueApi.GetQueueAsync(statusFilter, sortBy);
        _items = result?.Data;
    }

    private void NavigateToDetail(Guid applicationId)
    {
        NavigationManager.NavigateTo($"/review-queue/{applicationId}");
    }
}
```

---

## Review Flow: How Status Transitions Work

### Typical Happy Path

1. Borrower submits application → Status = `Submitted`
2. Admin opens review queue → sees application in list
3. Admin clicks application → navigates to `ReviewApplicationDetail.razor`
4. Admin clicks "Start Review" → `POST /mark-under-review` → Status = `UnderReview`
5. Admin verifies documents → `POST /documents/{docId}/verify`
6. Admin approves → `POST /approve` with reason → Status = `Approved`
7. Application appears in lender's funding queue

### Documents Requested Path

1. Admin reviews and finds missing documents
2. Admin clicks "Request Documents" with a note → Status = `DocumentsRequested`
3. Borrower sees notification, uploads additional documents
4. Borrower clicks "Resubmit" → Status = `UnderReview` (back in queue)
5. Admin reviews again → approves or rejects

---

## Step-by-Step Guide: Adding Bulk Actions

To add "Approve All Selected" functionality:

1. **Blazor** — Add checkboxes to each row, track selected IDs in a `HashSet<Guid>`
2. **API** — Add `POST /api/review-queue/bulk-approve` accepting `List<Guid>` + reason
3. **Application** — Create `BulkApproveLoanApplicationsCommand` that iterates and calls `Approve()` on each
4. **Validation** — Ensure all selected applications are in `UnderReview` status
5. **Error Handling** — Return partial success results if some fail


---

## Deep Dive: ReviewQueueApiClient

```csharp
public sealed class ReviewQueueApiClient
{
    private readonly HttpClient _httpClient;

    public ReviewQueueApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>?> GetQueueAsync(
        int? statusFilter, string? sortBy, CancellationToken ct = default)
    {
        var url = "api/review-queue";
        var queryParams = new List<string>();

        if (statusFilter.HasValue)
            queryParams.Add($"statusFilter={statusFilter.Value}");
        if (!string.IsNullOrEmpty(sortBy))
            queryParams.Add($"sortBy={sortBy}");

        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>>(
            url, ct);
    }

    public async Task<ApiResponse<ApplicationDetailDto>?> GetApplicationDetailsAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<ApplicationDetailDto>>(
            $"api/review-queue/{applicationId}", ct);
    }

    public async Task<ApiResponse<string>?> MarkUnderReviewAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/review-queue/{applicationId}/mark-under-review", null, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(ct);
    }

    public async Task<ApiResponse<string>?> ApproveAsync(
        Guid applicationId, string reason, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/approve",
            new ApproveRejectRequest { Reason = reason }, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(ct);
    }

    public async Task<ApiResponse<string>?> RejectAsync(
        Guid applicationId, string reason, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/reject",
            new ApproveRejectRequest { Reason = reason }, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(ct);
    }

    public async Task<ApiResponse<string>?> RequestDocumentsAsync(
        Guid applicationId, string note, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/review-queue/{applicationId}/request-documents",
            new RequestDocumentsRequest { Note = note }, ct);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(ct);
    }
}
```

---

## ReviewApplicationDetail.razor — Detail View

The detail page shows:
- Application summary (borrower, amount, term, purpose, status)
- Document list with verify/reject actions per document
- Action buttons based on current status
- Review history (who reviewed, when, reason)

### Conditional Action Buttons

```razor
@if (_detail.Status == (int)LoanApplicationStatus.Submitted)
{
    <button @onclick="MarkUnderReview">Start Review</button>
}

@if (_detail.Status == (int)LoanApplicationStatus.UnderReview)
{
    <button @onclick="OpenApproveModal">Approve</button>
    <button @onclick="OpenRejectModal">Reject</button>
    <button @onclick="OpenRequestDocsModal">Request Documents</button>
}
```

### Document Verification UI

```razor
@foreach (var doc in _detail.Documents)
{
    <div class="flex items-center justify-between p-3 border rounded-lg">
        <div>
            <span class="font-medium">@doc.FileName</span>
            <span class="text-xs text-slate-500 ml-2">@doc.Type</span>
        </div>
        <div class="flex gap-2">
            @if (doc.Status == "Pending")
            {
                <button @onclick="() => VerifyDocument(doc.Id)"
                        class="text-green-600 text-xs font-semibold">
                    ✓ Verify
                </button>
                <button @onclick="() => OpenRejectDocModal(doc.Id)"
                        class="text-red-600 text-xs font-semibold">
                    ✗ Reject
                </button>
            }
            else
            {
                <StatusBadge Status="@doc.Status" />
            }
        </div>
    </div>
}
```

---

## DTOs Used in Review Workflow

### ReviewQueueItemDto

```csharp
public sealed record ReviewQueueItemDto(
    Guid ApplicationId,
    string BorrowerName,
    decimal RequestedAmount,
    string ProductTitle,
    DateTime SubmittedAtUtc,
    int Status,
    int DocumentCount,
    int VerifiedDocumentCount);
```

### ApplicationDetailDto

```csharp
public sealed class ApplicationDetailDto
{
    public Guid Id { get; set; }
    public string BorrowerName { get; set; }
    public string BorrowerEmail { get; set; }
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public string Purpose { get; set; }
    public string ProductTitle { get; set; }
    public int Status { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DocumentRequestNote { get; set; }
    public IReadOnlyList<ApplicationDocumentDto> Documents { get; set; }
}
```

---

## Command Handlers

### MarkLoanApplicationUnderReviewCommand

```csharp
public sealed class MarkLoanApplicationUnderReviewCommandHandler
    : IRequestHandler<MarkLoanApplicationUnderReviewCommand, Unit>
{
    private readonly ILoanApplicationRepository _repository;

    public async Task<Unit> Handle(MarkLoanApplicationUnderReviewCommand request, CancellationToken ct)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, ct)
            ?? throw new DomainException("Application not found.");

        application.MarkUnderReview(); // Domain guard: must be Submitted

        await _repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

### ApproveLoanApplicationCommand

```csharp
public sealed class ApproveLoanApplicationCommandHandler
    : IRequestHandler<ApproveLoanApplicationCommand, Unit>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogRepository _auditRepository;

    public async Task<Unit> Handle(ApproveLoanApplicationCommand request, CancellationToken ct)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, ct)
            ?? throw new DomainException("Application not found.");

        var reviewerName = _currentUser.UserName ?? "System";

        application.Approve(request.Reason, reviewerName); // Domain guard: must be UnderReview

        await _auditRepository.AddAsync(
            AuditLog.Create("LoanApplication", application.Id, "Approved",
                $"Approved by {reviewerName}. Reason: {request.Reason}"), ct);

        await _repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

---

## Authorization & Security

The review queue is protected by:

1. **Controller-level**: `[Authorize(Policy = "CanProcessApplications")]`
2. **Blazor page**: `@attribute [Authorize(Roles = "Admin,CrmManager")]`
3. **Reviewer identity**: Captured from JWT claims and stored on the application

This ensures:
- Only authorized staff can access the review queue
- Every action is attributed to a specific reviewer
- Audit trail provides accountability

---

## Step-by-Step Guide: Adding Auto-Assignment

To automatically assign applications to reviewers:

1. **Domain** — Add `AssignedReviewerId` property to `LoanApplication`
2. **Application** — Create `AutoAssignReviewerService` that distributes load
3. **Handler** — In `MarkUnderReview`, call auto-assignment service
4. **Query** — Filter review queue by assigned reviewer
5. **Blazor** — Show "My Queue" vs "All Queue" tabs
