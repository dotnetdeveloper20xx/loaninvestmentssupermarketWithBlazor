# 15 — Loan Application Wizard (Frontend)

## Feature Requirements

The Loan Application Wizard is a multi-step form that guides borrowers through creating and submitting a loan application. Steps:

1. **Step 1 — Loan Parameters**: Enter amount, term, and purpose → creates a draft application
2. **Step 2 — Product Matching**: System finds matching products based on amount/term/credit tier
3. **Step 3 — Product Selection**: Borrower picks a product from matched results
4. **Step 4 — Document Upload**: Upload supporting documents (ID, proof of income, etc.)
5. **Step 5 — Review & Submit**: Review all details and submit the application

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| Blazor WASM | Client-side SPA with component-based architecture |
| WizardStateService | Singleton service managing wizard state across steps |
| WizardApiClient | Typed HttpClient for all wizard API calls |
| Component Composition | Each step is a separate Razor component |
| `MultipartFormDataContent` | File upload with streaming |

---

## WizardStateService

This service maintains the wizard's state across step navigation. It's registered as a scoped service in DI.

```csharp
namespace LoanSuperMarket.Blazor.Services;

public sealed class WizardStateService
{
    public Guid? ApplicationId { get; private set; }
    public int CurrentStep { get; private set; } = 1;

    // Data from Step 1
    public string Purpose { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }

    // Data from Step 3
    public string? SelectedProductTitle { get; set; }

    public void SetApplicationId(Guid id)
    {
        ApplicationId = id;
    }

    public void GoToStep(int step)
    {
        if (step < 1 || step > 5)
            return;
        CurrentStep = step;
    }

    public void Reset()
    {
        ApplicationId = null;
        CurrentStep = 1;
        Purpose = string.Empty;
        RequestedAmount = 0;
        TermMonths = 0;
        SelectedProductTitle = null;
    }
}
```

### Key Design Decisions

- **`ApplicationId`** is set after Step 1 creates the draft. All subsequent steps use this ID.
- **`GoToStep`** bounds-checks to prevent invalid navigation.
- **`Reset()`** clears everything when starting a new application.
- **Step data is stored** so the review step can display it without re-fetching.

---

## LoanApplicationWizard.razor — Orchestrator Page

```razor
@page "/wizard"
@page "/wizard/{ApplicationId:guid}"
@using LoanSuperMarket.Blazor.Components.LoanApplications.Wizard
@attribute [Authorize(Roles = "Borrower")]
@inject WizardStateService WizardState
@inject WizardApiClient WizardApi
@inject NavigationManager NavigationManager

<div class="max-w-4xl mx-auto">
    <div class="mb-6">
        <h1 class="text-2xl font-bold text-slate-900">Loan Application</h1>
        <p class="text-sm text-slate-500 mt-1">Complete the steps below to submit your loan application.</p>
    </div>

    <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 lg:p-8">
        <WizardStepIndicator CurrentStep="@WizardState.CurrentStep" TotalSteps="5" />

        @switch (WizardState.CurrentStep)
        {
            case 1:
                <Step1_LoanParameters OnNext="HandleStepAdvance" />
                break;
            case 2:
                <Step2_ProductMatching OnNext="HandleStepAdvance" OnBack="HandleStepBack" />
                break;
            case 3:
                <Step3_ProductSelection OnNext="HandleStepAdvance" OnBack="HandleStepBack" />
                break;
            case 4:
                <Step4_DocumentUpload OnNext="HandleStepAdvance" OnBack="HandleStepBack" />
                break;
            case 5:
                <Step5_ReviewSubmit OnBack="HandleStepBack" />
                break;
        }
    </div>
</div>

@code {
    [Parameter] public Guid? ApplicationId { get; set; }

    protected override void OnInitialized()
    {
        if (ApplicationId.HasValue)
        {
            WizardState.SetApplicationId(ApplicationId.Value);
        }
        else if (WizardState.ApplicationId is null)
        {
            WizardState.Reset();
        }
    }

    private void HandleStepAdvance()
    {
        WizardState.GoToStep(WizardState.CurrentStep + 1);
        StateHasChanged();
    }

    private void HandleStepBack()
    {
        WizardState.GoToStep(WizardState.CurrentStep - 1);
        StateHasChanged();
    }
}
```

### How It Works

1. **Dual routes**: `/wizard` for new applications, `/wizard/{id}` to resume existing drafts
2. **Step rendering**: Uses `@switch` on `WizardState.CurrentStep` to render the active step component
3. **Navigation callbacks**: Each step calls `OnNext`/`OnBack` EventCallbacks to advance/retreat
4. **State persistence**: `WizardStateService` survives navigation between steps (scoped lifetime)

---

## WizardApiClient — HTTP Communication Layer

```csharp
public sealed class WizardApiClient
{
    private readonly HttpClient _httpClient;

    public WizardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Step 1: Create draft application
    public async Task<ApiResponse<Guid>?> CreateDraftAsync(
        CreateDraftRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/wizard/create-draft", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(cancellationToken);
    }

    // Step 1 (update): Modify draft parameters
    public async Task<ApiResponse<string>?> UpdateParametersAsync(
        Guid applicationId, UpdateDraftParametersRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/wizard/{applicationId}/parameters", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Step 2: Match products for the application
    public async Task<ApiResponse<IReadOnlyList<MatchedProductDto>>?> MatchProductsAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/match-products",
            content: null, cancellationToken);
        return await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<MatchedProductDto>>>(cancellationToken);
    }

    // Step 3: Select a product
    public async Task<ApiResponse<string>?> SelectProductAsync(
        Guid applicationId, SelectProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/wizard/{applicationId}/select-product", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Step 4: Upload a document
    public async Task<ApiResponse<Guid>?> UploadDocumentAsync(
        Guid applicationId, Stream fileStream, string fileName,
        int documentType, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(documentType.ToString()), "documentType");

        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/documents", content, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(cancellationToken);
    }

    // Step 4: Remove a document
    public async Task<ApiResponse<string>?> RemoveDocumentAsync(
        Guid applicationId, Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(
            $"api/wizard/{applicationId}/documents/{documentId}", cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Step 4: Get uploaded documents
    public async Task<ApiResponse<IReadOnlyList<ApplicationDocumentDto>>?> GetDocumentsAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<ApiResponse<IReadOnlyList<ApplicationDocumentDto>>>(
                $"api/wizard/{applicationId}/documents", cancellationToken);
    }

    // Step 5: Submit the application
    public async Task<ApiResponse<string>?> SubmitAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/submit", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Withdraw a draft application
    public async Task<ApiResponse<string>?> WithdrawAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/withdraw", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Resubmit after documents requested
    public async Task<ApiResponse<string>?> ResubmitAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/wizard/{applicationId}/resubmit", content: null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(cancellationToken);
    }

    // Get all borrower's applications
    public async Task<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>?>
        GetBorrowerApplicationsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>>(
                "api/borrower/applications", cancellationToken);
    }
}
```

---

## Step-by-Step Flow

### Step 1: Create Draft (API Call)

```
User fills form → Step1 component calls:
    WizardApi.CreateDraftAsync(new CreateDraftRequest {
        RequestedAmount = 10000,
        TermMonths = 24,
        Purpose = "Home renovation"
    })
    → POST /api/wizard/create-draft
    → Returns ApplicationId (Guid)
    → WizardState.SetApplicationId(id)
    → OnNext() → advance to Step 2
```

### Step 2: Match Products

```
Step2 component calls:
    WizardApi.MatchProductsAsync(WizardState.ApplicationId)
    → POST /api/wizard/{id}/match-products
    → Server runs ProductMatchingService
    → Returns list of MatchedProductDto sorted by effective rate
    → Display products as cards
    → OnNext() → advance to Step 3
```

### Step 3: Select Product

```
User clicks a product card → Step3 calls:
    WizardApi.SelectProductAsync(id, new SelectProductRequest { LoanProductId = productId })
    → PUT /api/wizard/{id}/select-product
    → Domain: application.SelectProduct(productId)
    → OnNext() → advance to Step 4
```

### Step 4: Document Upload (with Buffering Fix)

```
User selects file → Step4 calls:
    WizardApi.UploadDocumentAsync(id, fileStream, fileName, documentType)
    → POST /api/wizard/{id}/documents (multipart/form-data)
    → Server: await using var stream = file.OpenReadStream()
    → Stores file via IDocumentStorageService
    → Creates ApplicationDocument entity
    → Returns document ID
```

**Buffering Fix**: The API uses `await using var stream = file.OpenReadStream()` to properly dispose the stream. The Blazor client uses `StreamContent` wrapping the file stream directly — avoiding loading the entire file into memory.

### Step 5: Review & Submit

```
Step5 displays summary from WizardState:
    - Amount, Term, Purpose (from Step 1 state)
    - Selected product title (from Step 3 state)
    - Document list (fetched via GetDocumentsAsync)

User clicks Submit → Step5 calls:
    WizardApi.SubmitAsync(id)
    → POST /api/wizard/{id}/submit
    → Domain: application.Submit()
    → Status changes: Draft → Submitted
    → Navigate to applications list
```

---

## API Layer: `WizardController.cs`

The controller maps each wizard step to a specific endpoint:

| Step | Method | Route | Purpose |
|---|---|---|---|
| 1 | POST | `/api/wizard/create-draft` | Create draft application |
| 1 | PUT | `/api/wizard/{id}/parameters` | Update draft parameters |
| 2 | POST | `/api/wizard/{id}/match-products` | Find matching products |
| 3 | PUT | `/api/wizard/{id}/select-product` | Associate product |
| 4 | POST | `/api/wizard/{id}/documents` | Upload document |
| 4 | DELETE | `/api/wizard/{id}/documents/{docId}` | Remove document |
| 4 | GET | `/api/wizard/{id}/documents` | List documents |
| 5 | POST | `/api/wizard/{id}/submit` | Submit application |
| — | POST | `/api/wizard/{id}/withdraw` | Withdraw draft |
| — | POST | `/api/wizard/{id}/resubmit` | Resubmit after docs requested |

---

## Step-by-Step Guide: Adding a New Wizard Step

Example: Adding Step 2.5 — "Affordability Check" between matching and selection.

1. **Create component**: `Components/LoanApplications/Wizard/Step2b_AffordabilityCheck.razor`
2. **Update WizardStateService**: Change max step from 5 to 6, add affordability data properties
3. **Update orchestrator**: Add `case 3:` for the new step, shift existing steps 3-5 to 4-6
4. **Add API endpoint** (if needed): `POST /api/wizard/{id}/affordability-check`
5. **Update WizardApiClient**: Add `CheckAffordabilityAsync()` method
6. **Update step indicator**: Change `TotalSteps="6"`


---

## Deep Dive: Step Components

### Step 1 — Loan Parameters Component

The first step collects the basic loan requirements from the borrower:

```razor
@* Step1_LoanParameters.razor *@
<div class="space-y-6 mt-6">
    <h2 class="text-lg font-semibold text-slate-800">Loan Details</h2>
    <p class="text-sm text-slate-500">Tell us about the loan you need.</p>

    <EditForm Model="_model" OnValidSubmit="HandleSubmit">
        <DataAnnotationsValidator />

        <AppCurrencyInput Label="How much do you need?"
                          @bind-Value="_model.RequestedAmount"
                          Min="500" Max="100000" />

        <AppLoanTermInput Label="Repayment term"
                          @bind-Value="_model.TermMonths"
                          MinMonths="3" MaxMonths="60" />

        <AppTextArea Label="What is the loan for?"
                     @bind-Value="_model.Purpose"
                     Placeholder="e.g., Home renovation, debt consolidation..."
                     MaxLength="1000" />

        <FormActions>
            <button type="submit" disabled="@_isSubmitting"
                    class="rounded-xl bg-blue-600 px-6 py-3 text-sm font-semibold text-white">
                @(_isSubmitting ? "Creating..." : "Continue")
            </button>
        </FormActions>
    </EditForm>
</div>

@code {
    [Parameter] public EventCallback OnNext { get; set; }

    private CreateDraftModel _model = new();
    private bool _isSubmitting;

    private async Task HandleSubmit()
    {
        _isSubmitting = true;
        try
        {
            if (WizardState.ApplicationId is null)
            {
                // First time — create draft
                var response = await WizardApi.CreateDraftAsync(new CreateDraftRequest
                {
                    RequestedAmount = _model.RequestedAmount,
                    TermMonths = _model.TermMonths,
                    Purpose = _model.Purpose
                });

                if (response?.Success == true)
                {
                    WizardState.SetApplicationId(response.Data);
                    WizardState.RequestedAmount = _model.RequestedAmount;
                    WizardState.TermMonths = _model.TermMonths;
                    WizardState.Purpose = _model.Purpose;
                    await OnNext.InvokeAsync();
                }
            }
            else
            {
                // Returning — update existing draft
                await WizardApi.UpdateParametersAsync(
                    WizardState.ApplicationId.Value,
                    new UpdateDraftParametersRequest
                    {
                        RequestedAmount = _model.RequestedAmount,
                        TermMonths = _model.TermMonths,
                        Purpose = _model.Purpose
                    });
                await OnNext.InvokeAsync();
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private class CreateDraftModel
    {
        public decimal RequestedAmount { get; set; } = 5000;
        public int TermMonths { get; set; } = 12;
        public string Purpose { get; set; } = string.Empty;
    }
}
```

### Step 2 — Product Matching Component

```razor
@* Step2_ProductMatching.razor *@
<div class="space-y-6 mt-6">
    <h2 class="text-lg font-semibold text-slate-800">Available Products</h2>
    <p class="text-sm text-slate-500">
        Based on your requirements, here are the products available to you.
    </p>

    @if (_isLoading)
    {
        <LoadingSkeleton Variant="cards" Columns="3" />
    }
    else if (_products.Count == 0)
    {
        <EmptyState Icon="🔍"
                    Title="No matching products"
                    Message="No products match your loan amount and term. Try adjusting your parameters." />
    }
    else
    {
        <div class="grid gap-4 md:grid-cols-2">
            @foreach (var product in _products)
            {
                <div class="rounded-xl border border-slate-200 p-5 hover:border-blue-300 transition">
                    <div class="font-semibold text-slate-900">@product.Title</div>
                    <div class="text-sm text-slate-500 mt-1">@product.LenderName</div>
                    <div class="mt-3 flex items-baseline gap-1">
                        <span class="text-2xl font-bold text-blue-600">
                            @product.EffectiveInterestRate.ToString("N2")%
                        </span>
                        <span class="text-xs text-slate-500">effective rate</span>
                    </div>
                    <div class="mt-2 text-xs text-slate-500">
                        £@product.MinimumAmount.ToString("N0") - £@product.MaximumAmount.ToString("N0")
                        · @product.MinimumTermMonths-@product.MaximumTermMonths months
                    </div>
                </div>
            }
        </div>
    }

    <FormActions>
        <button @onclick="OnBack.InvokeAsync" class="btn-secondary">Back</button>
        <button @onclick="OnNext.InvokeAsync" class="btn-primary"
                disabled="@(_products.Count == 0)">Continue</button>
    </FormActions>
</div>

@code {
    [Parameter] public EventCallback OnNext { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }

    private List<MatchedProductDto> _products = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        if (WizardState.ApplicationId is null) return;

        var response = await WizardApi.MatchProductsAsync(WizardState.ApplicationId.Value);
        if (response?.Success == true)
            _products = response.Data?.ToList() ?? [];

        _isLoading = false;
    }
}
```

### Step 4 — Document Upload (Buffering Fix Detail)

The document upload step handles file uploads in Blazor WASM. The key challenge is that Blazor's `InputFile` component provides an `IBrowserFile` which must be read as a stream:

```csharp
// In the Step4 component:
private async Task HandleFileSelected(InputFileChangeEventArgs e)
{
    var file = e.File;
    if (file.Size > 10 * 1024 * 1024) // 10MB limit
    {
        _error = "File size exceeds 10MB limit.";
        return;
    }

    _isUploading = true;
    try
    {
        // BUFFERING FIX: Use OpenReadStream with maxAllowedSize
        // Default is 512KB — we need to increase for document uploads
        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);

        var response = await WizardApi.UploadDocumentAsync(
            WizardState.ApplicationId!.Value,
            stream,
            file.Name,
            (int)_selectedDocType);

        if (response?.Success == true)
        {
            await LoadDocuments(); // Refresh document list
        }
    }
    finally
    {
        _isUploading = false;
    }
}
```

**The Buffering Fix**: By default, `IBrowserFile.OpenReadStream()` limits to 512KB. For document uploads (PDFs, images), you must pass `maxAllowedSize` to allow larger files. The stream is then wrapped in `StreamContent` by the `WizardApiClient` and sent as `multipart/form-data`.

---

## Error Handling Patterns

Each step handles errors consistently:

```csharp
try
{
    var response = await WizardApi.SomeMethodAsync(...);
    if (response is null)
    {
        _error = "No response from server. Please try again.";
        return;
    }
    if (!response.Success)
    {
        _error = response.Errors.FirstOrDefault() ?? "An error occurred.";
        return;
    }
    // Success path
}
catch (HttpRequestException)
{
    _error = "Network error. Please check your connection.";
}
catch (Exception ex)
{
    _error = $"Unexpected error: {ex.Message}";
}
```

---

## DI Registration

In `Program.cs` of the Blazor project:

```csharp
builder.Services.AddScoped<WizardStateService>();
builder.Services.AddHttpClient<WizardApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});
```

The `WizardStateService` is **scoped** (per-circuit in Blazor Server, per-tab in WASM), ensuring each user session has its own wizard state.
