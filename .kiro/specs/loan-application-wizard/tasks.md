# Implementation Plan: Loan Application Wizard

## Overview

This plan implements the Loan Application Wizard feature by enhancing the existing `LoanApplication` entity with Draft-first creation and a `DocumentsRequested` state, adding a new `ApplicationDocument` entity, building a `ProductMatchingService`, creating CQRS commands/queries for the wizard and CRM review flows, exposing new API endpoints, and building the multi-step Blazor WASM wizard UI with a CRM review queue.

## Tasks

- [ ] 1. Domain layer enhancements — enums, entities, and state machine
  - [ ] 1.1 Add `DocumentsRequested = 8` to `LoanApplicationStatus` enum and create `DocumentType` and `DocumentStatus` enums
    - Add `DocumentsRequested = 8` to `src/LoanSuperMarket.Domain/Enums/LoanApplicationStatus.cs`
    - Create `src/LoanSuperMarket.Domain/Enums/DocumentType.cs` with values: NationalID=1, ProofOfIncome=2, BankStatement=3, AddressProof=4, Other=5
    - Create `src/LoanSuperMarket.Domain/Enums/DocumentStatus.cs` with values: Pending=1, Verified=2, Rejected=3
    - _Requirements: 6.1, 11.2_

  - [ ] 1.2 Enhance `LoanApplication` entity with Draft-first creation and new state transitions
    - Add nullable `LoanProductId` (change from required to nullable `Guid?`)
    - Add nullable `SubmittedAtUtc` (change from always-set to nullable `DateTime?`)
    - Add properties: `ReviewedBy` (string?), `ReviewReason` (string?), `ReviewedAtUtc` (DateTime?), `DocumentRequestNote` (string?)
    - Add `CreateDraft(Guid borrowerId, Money requestedAmount, int termMonths, string purpose)` factory method that sets Status=Draft without requiring LoanProductId
    - Add `UpdateParameters(Money amount, int termMonths, string purpose)` method for updating draft
    - Add `SelectProduct(Guid loanProductId)` method for associating product with draft
    - Add `Submit()` method that validates completeness (product selected) and transitions Draft→Submitted, sets SubmittedAtUtc
    - Modify `Approve(string reason, string reviewedBy)` to require reason and reviewer, set ReviewedBy/ReviewReason/ReviewedAtUtc
    - Modify `Reject(string reason, string reviewedBy)` to require reason and reviewer, set ReviewedBy/ReviewReason/ReviewedAtUtc
    - Add `RequestDocuments(string note, string requestedBy)` method: UnderReview→DocumentsRequested, sets DocumentRequestNote
    - Add `ResubmitForReview()` method: DocumentsRequested→UnderReview
    - Restrict `Withdraw()` to only work from Draft status
    - Enforce all state transitions per design state machine with DomainException on invalid transitions
    - _Requirements: 1.1, 1.3, 1.4, 1.5, 5.2, 6.1, 6.2, 6.3, 6.4, 9.1, 9.2, 9.3, 10.1_

  - [ ] 1.3 Create `ApplicationDocument` entity
    - Create `src/LoanSuperMarket.Domain/Entities/ApplicationDocument.cs` extending `AuditableEntity`
    - Properties: `LoanApplicationId` (Guid), `Type` (DocumentType), `FileName` (string), `StorageReference` (string), `UploadedAtUtc` (DateTime), `Status` (DocumentStatus), `VerifiedBy` (string?), `VerifiedAtUtc` (DateTime?), `RejectionNote` (string?)
    - Add `Create(Guid loanApplicationId, DocumentType type, string fileName, string storageReference)` factory method setting Status=Pending, UploadedAtUtc=UtcNow
    - Add `Verify(string verifiedBy)` method: Pending→Verified, sets VerifiedBy and VerifiedAtUtc
    - Add `Reject(string rejectedBy, string rejectionNote)` method: Pending→Rejected, validates note is not empty, sets VerifiedBy/VerifiedAtUtc/RejectionNote
    - Throw DomainException if Verify/Reject called on non-Pending document
    - _Requirements: 8.1, 8.2, 8.3, 11.1, 11.2, 11.3_

- [ ] 2. Shared layer — DTOs and request models
  - [ ] 2.1 Create wizard-related DTOs and request models in the Shared project
    - Create `src/LoanSuperMarket.Shared/LoanApplications/MatchedProductDto.cs` — record with ProductId, Title, LenderName, EffectiveInterestRate, MinimumAmount, MaximumAmount, MinimumTermMonths, MaximumTermMonths
    - Create `src/LoanSuperMarket.Shared/LoanApplications/ApplicationDocumentDto.cs` — record with Id, FileName, Type, Status, UploadedAtUtc, VerifiedBy, VerifiedAtUtc, RejectionNote
    - Create `src/LoanSuperMarket.Shared/LoanApplications/ReviewQueueItemDto.cs` — record with ApplicationId, BorrowerName, RequestedAmount, ProductTitle, SubmittedAtUtc, Status, DocumentCount, VerifiedDocumentCount
    - Create `src/LoanSuperMarket.Shared/LoanApplications/WizardApplicationSummaryDto.cs` — record with ApplicationId, ProductTitle, RequestedAmount, TermMonths, SubmittedAtUtc, Status, MatchedProductCount, UploadedDocuments, VerifiedDocuments, RejectedDocuments
    - Create `src/LoanSuperMarket.Shared/LoanApplications/ApplicationDetailDto.cs` — full application details for review queue
    - Create `src/LoanSuperMarket.Shared/LoanApplications/CreateDraftRequest.cs` — RequestedAmount, TermMonths, Purpose
    - Create `src/LoanSuperMarket.Shared/LoanApplications/UpdateDraftParametersRequest.cs` — RequestedAmount, TermMonths, Purpose
    - Create `src/LoanSuperMarket.Shared/LoanApplications/SelectProductRequest.cs` — LoanProductId
    - Create `src/LoanSuperMarket.Shared/LoanApplications/ApproveRejectRequest.cs` — Reason
    - Create `src/LoanSuperMarket.Shared/LoanApplications/RequestDocumentsRequest.cs` — Note
    - Create `src/LoanSuperMarket.Shared/LoanApplications/RejectDocumentRequest.cs` — RejectionNote
    - _Requirements: 2.5, 4.7, 7.1, 7.2, 12.1_

- [ ] 3. Application layer — interfaces, services, and CQRS handlers
  - [ ] 3.1 Create `IDocumentStorageService` and `IApplicationDocumentRepository` interfaces
    - Create `src/LoanSuperMarket.Application/Common/Interfaces/IDocumentStorageService.cs` with methods: StoreAsync(Stream, string, CancellationToken) → string, RetrieveAsync(string, CancellationToken) → Stream, DeleteAsync(string, CancellationToken)
    - Create `src/LoanSuperMarket.Application/Common/Interfaces/IApplicationDocumentRepository.cs` with methods: AddAsync, GetByIdAsync, GetByApplicationIdAsync, SaveChangesAsync
    - _Requirements: 4.3, 4.4, 11.4, 11.5_

  - [ ] 3.2 Create `ProductMatchingService` in the Application layer
    - Create `src/LoanSuperMarket.Application/Features/LoanApplications/ProductMatching/ProductMatchingService.cs`
    - Inject `ILoanProductRepository`
    - Implement `MatchProductsAsync(decimal requestedAmount, int requestedTermMonths, CreditTier borrowerTier, CancellationToken ct)` returning `IReadOnlyList<MatchedProductDto>`
    - Filter published products where MinimumAmount <= requestedAmount <= MaximumAmount AND MinimumTermMonths <= requestedTermMonths <= MaximumTermMonths
    - Calculate effective rate: Tier A = base rate, Tier B = base + 2, Tier C = base + 4
    - Sort by effective rate ASC, then MaximumAmount DESC for ties
    - Register in DI
    - _Requirements: 2.1, 2.2, 2.3, 15.1, 15.2, 15.3, 15.4, 15.5_

  - [ ] 3.3 Create wizard CQRS commands — CreateDraft, UpdateDraft, SelectProduct, Submit, Withdraw
    - Create `src/LoanSuperMarket.Application/Features/LoanApplications/CreateDraftLoanApplication/` folder with Command, Handler, and Validator
    - Command: BorrowerId (resolved from current user), RequestedAmount, TermMonths, Purpose → returns Guid
    - Handler: resolve BorrowerId from ICurrentUserService + IBorrowerRepository, call LoanApplication.CreateDraft(), persist
    - Validator: Amount > 0, TermMonths > 0, Purpose not empty and ≤ 1000 chars
    - Create `UpdateDraftLoanApplication/` with Command (ApplicationId, Amount, TermMonths, Purpose), Handler, Validator
    - Create `SelectProduct/` with Command (ApplicationId, LoanProductId), Handler
    - Create `SubmitLoanApplication/` with Command (ApplicationId), Handler that validates required documents exist then calls Submit()
    - Create `WithdrawLoanApplication/` with Command (ApplicationId), Handler
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 3.1, 3.2, 3.3, 5.2, 5.5_

  - [ ] 3.4 Create document CQRS commands — Upload, Remove, Verify, Reject
    - Create `src/LoanSuperMarket.Application/Features/LoanApplications/UploadDocument/` with Command (ApplicationId, DocumentType, FileName, FileStream), Handler, Validator
    - Handler: validate application exists and is in Draft or DocumentsRequested status, call IDocumentStorageService.StoreAsync, create ApplicationDocument, persist
    - Create `RemoveDocument/` with Command (ApplicationId, DocumentId), Handler that deletes from storage and repository
    - Create `VerifyDocument/` with Command (ApplicationId, DocumentId), Handler that calls document.Verify(currentUser)
    - Create `RejectDocument/` with Command (ApplicationId, DocumentId, RejectionNote), Handler, Validator (note required)
    - _Requirements: 4.3, 4.4, 4.6, 8.1, 8.2, 8.3, 11.2, 11.3, 11.4_

  - [ ] 3.5 Create CRM review CQRS commands — RequestDocuments, ResubmitForReview, enhanced Approve/Reject
    - Create `RequestAdditionalDocuments/` with Command (ApplicationId, Note), Handler, Validator (note required)
    - Create `ResubmitForReview/` with Command (ApplicationId), Handler
    - Modify existing `ApproveLoanApplication` command to accept Reason parameter, update handler to call Approve(reason, reviewedBy)
    - Modify existing `RejectLoanApplication` command to accept Reason parameter, update handler to call Reject(reason, reviewedBy)
    - _Requirements: 6.2, 6.3, 9.1, 9.2, 9.3, 9.4, 10.1, 10.2, 10.3, 10.4_

  - [ ] 3.6 Create queries — MatchProducts, GetReviewQueue, GetApplicationDetails, GetBorrowerApplications
    - Create `MatchProducts/` query with handler that calls ProductMatchingService (takes ApplicationId, resolves amount/term/tier from application + borrower)
    - Create `GetReviewQueue/` query implementing IResourceFilteredQuery, returns list of ReviewQueueItemDto, filters by Submitted/UnderReview/DocumentsRequested statuses, supports sorting and status filtering
    - Create `GetApplicationDetails/` query returning ApplicationDetailDto with documents, borrower info, product info
    - Create `GetBorrowerApplications/` query implementing IResourceFilteredQuery, returns list of WizardApplicationSummaryDto for the current borrower
    - Create `GetApplicationDocuments/` query returning list of ApplicationDocumentDto for a given application
    - _Requirements: 2.4, 2.5, 7.1, 7.2, 7.3, 7.4, 12.1, 12.2, 12.3, 12.4, 13.1, 13.2, 13.3, 14.3, 14.4_

- [ ] 4. Infrastructure layer — persistence, repository, and stub storage
  - [ ] 4.1 Add `ApplicationDocument` EF Core configuration and update `ApplicationDbContext`
    - Create `src/LoanSuperMarket.Infrastructure/Persistence/Configurations/ApplicationDocumentConfiguration.cs` with table name, column types, FK to LoanApplications
    - Update `ApplicationDbContext` to add `DbSet<ApplicationDocument>`
    - Update `LoanApplication` EF configuration for nullable LoanProductId, new columns (ReviewedBy, ReviewReason, ReviewedAtUtc, DocumentRequestNote)
    - Add navigation property `ICollection<ApplicationDocument> Documents` to LoanApplication if needed for EF queries
    - _Requirements: 11.1_

  - [ ] 4.2 Create EF Core migration for schema changes
    - Add migration for: LoanApplicationStatus.DocumentsRequested, nullable LoanProductId, new LoanApplication columns, new ApplicationDocuments table
    - _Requirements: 6.1, 11.1_

  - [ ] 4.3 Implement `ApplicationDocumentRepository`
    - Create `src/LoanSuperMarket.Infrastructure/Repositories/ApplicationDocumentRepository.cs` implementing `IApplicationDocumentRepository`
    - Implement AddAsync, GetByIdAsync, GetByApplicationIdAsync, SaveChangesAsync using ApplicationDbContext
    - _Requirements: 11.4, 11.5_

  - [ ] 4.4 Implement stub `DocumentStorageService`
    - Create `src/LoanSuperMarket.Infrastructure/Services/StubDocumentStorageService.cs` implementing `IDocumentStorageService`
    - StoreAsync: generate a unique storage reference (GUID-based path), write file to a local `App_Data/documents/` folder, return the reference
    - RetrieveAsync: read file from local path by storage reference
    - DeleteAsync: delete file from local path
    - Register in Infrastructure DI
    - _Requirements: 4.4_

  - [ ] 4.5 Update `ILoanApplicationRepository` and its implementation for new query needs
    - Add `GetByIdWithDocumentsAsync(Guid id, CancellationToken ct)` to include documents
    - Add `GetByBorrowerIdAsync(Guid borrowerId, CancellationToken ct)` for borrower dashboard
    - Add `GetReviewQueueAsync(LoanApplicationStatus[]? statusFilter, string? sortBy, CancellationToken ct)` for CRM queue
    - Update `LoanApplicationRepository` implementation with EF Core queries including joins to Borrower, LoanProduct, and ApplicationDocuments
    - _Requirements: 7.1, 7.3, 7.4, 12.1, 14.3_

  - [ ] 4.6 Register new services in Infrastructure `DependencyInjection.cs`
    - Register `IApplicationDocumentRepository` → `ApplicationDocumentRepository`
    - Register `IDocumentStorageService` → `StubDocumentStorageService`
    - Register `ProductMatchingService` (or in Application DI)
    - _Requirements: 3.2, 4.4, 11.4_

- [ ] 5. API layer — Wizard and Review Queue controllers
  - [ ] 5.1 Create `WizardController` with borrower-facing endpoints
    - Create `src/LoanSuperMarket.Api/Controllers/WizardController.cs`
    - `[Authorize(Roles = "Borrower")]` on controller
    - POST `/api/wizard/create-draft` → CreateDraftLoanApplicationCommand → ApiResponse<Guid>
    - PUT `/api/wizard/{id}/parameters` → UpdateDraftLoanApplicationCommand → ApiResponse<string>
    - POST `/api/wizard/{id}/match-products` → MatchProductsQuery → ApiResponse<List<MatchedProductDto>>
    - PUT `/api/wizard/{id}/select-product` → SelectProductCommand → ApiResponse<string>
    - POST `/api/wizard/{id}/documents` (multipart form) → UploadDocumentCommand → ApiResponse<Guid>
    - DELETE `/api/wizard/{id}/documents/{docId}` → RemoveDocumentCommand → ApiResponse<string>
    - POST `/api/wizard/{id}/submit` → SubmitLoanApplicationCommand → ApiResponse<string>
    - POST `/api/wizard/{id}/withdraw` → WithdrawLoanApplicationCommand → ApiResponse<string>
    - POST `/api/wizard/{id}/resubmit` → ResubmitForReviewCommand → ApiResponse<string>
    - GET `/api/borrower/applications` → GetBorrowerApplicationsQuery → ApiResponse<List<WizardApplicationSummaryDto>>
    - _Requirements: 1.1, 1.5, 2.5, 3.3, 4.3, 4.6, 5.2, 10.3, 10.4, 12.1, 14.1_

  - [ ] 5.2 Create `ReviewQueueController` with CRM-facing endpoints
    - Create `src/LoanSuperMarket.Api/Controllers/ReviewQueueController.cs`
    - `[Authorize(Policy = "CanProcessApplications")]` on controller
    - GET `/api/review-queue` → GetReviewQueueQuery → ApiResponse<List<ReviewQueueItemDto>>
    - GET `/api/review-queue/{id}` → GetApplicationDetailsQuery → ApiResponse<ApplicationDetailDto>
    - POST `/api/review-queue/{id}/mark-under-review` → MarkLoanApplicationUnderReviewCommand → ApiResponse<string>
    - POST `/api/review-queue/{id}/approve` → ApproveLoanApplicationCommand → ApiResponse<string>
    - POST `/api/review-queue/{id}/reject` → RejectLoanApplicationCommand → ApiResponse<string>
    - POST `/api/review-queue/{id}/request-documents` → RequestAdditionalDocumentsCommand → ApiResponse<string>
    - POST `/api/review-queue/{id}/documents/{docId}/verify` → VerifyDocumentCommand → ApiResponse<string>
    - POST `/api/review-queue/{id}/documents/{docId}/reject` → RejectDocumentCommand → ApiResponse<string>
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.4, 9.1, 9.2, 9.3, 9.4, 10.1, 14.2, 14.6, 14.7_

- [ ] 6. Blazor WASM — API clients and wizard state service
  - [ ] 6.1 Create `WizardApiClient` and `ReviewQueueApiClient` services
    - Create `src/LoanSuperMarket.Blazor/Services/ApiClients/WizardApiClient.cs` with methods for all wizard endpoints (CreateDraft, UpdateParameters, MatchProducts, SelectProduct, UploadDocument, RemoveDocument, Submit, Withdraw, Resubmit, GetBorrowerApplications)
    - Create `src/LoanSuperMarket.Blazor/Services/ApiClients/ReviewQueueApiClient.cs` with methods for all review queue endpoints
    - Follow existing HttpClient injection pattern from other ApiClient classes
    - Register in Blazor `Program.cs`
    - _Requirements: 1.1, 2.5, 7.1_

  - [ ] 6.2 Create `WizardStateService` for client-side wizard state management
    - Create `src/LoanSuperMarket.Blazor/Services/WizardStateService.cs`
    - Properties: ApplicationId (Guid?), CurrentStep (int, default 1), FormModel (WizardFormModel)
    - Methods: SetApplicationId, GoToStep, Reset, CanAdvance (validates current step)
    - Create `WizardFormModel` class with Purpose, RequestedAmount, TermMonths, SelectedProductId, UploadedDocuments list
    - Register as Scoped service in Program.cs
    - _Requirements: 1.1, 3.4, 5.4_

- [ ] 7. Blazor WASM — Wizard UI components
  - [ ] 7.1 Create `WizardStepIndicator.razor` and `LoanApplicationWizard.razor` container
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/WizardStepIndicator.razor` showing steps 1-5 with active/completed states using Tailwind CSS
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/LoanApplicationWizard.razor` as the parent container that renders the step indicator and the current step component based on WizardStateService.CurrentStep
    - Wire up navigation between steps (Next/Back buttons)
    - _Requirements: 1.1, 3.4, 5.4_

  - [ ] 7.2 Create `Step1_LoanParameters.razor` component
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/Step1_LoanParameters.razor`
    - Form fields: Purpose (textarea, max 1000 chars), RequestedAmount (decimal input, > 0), TermMonths (int input, > 0)
    - Client-side validation with error messages per requirement 1.2, 1.3, 1.4
    - On "Next": call WizardApiClient.CreateDraft (or UpdateParameters if resuming), advance to step 2
    - Use Tailwind CSS for styling consistent with existing components
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [ ] 7.3 Create `Step2_ProductMatching.razor` component
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/Step2_ProductMatching.razor`
    - On load: call WizardApiClient.MatchProducts, display results
    - Display each product: Title, LenderName, EffectiveInterestRate, amount range, term range
    - Show "no matching products" message with suggestion to adjust amount/term when list is empty
    - Allow navigation back to Step 1
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 7.4 Create `Step3_ProductSelection.razor` component
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/Step3_ProductSelection.razor`
    - Display matched products as selectable cards/radio buttons
    - Validate that a product is selected before advancing (show error per requirement 3.2)
    - On "Next": call WizardApiClient.SelectProduct, advance to step 4
    - Allow navigation back to Step 2
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [ ] 7.5 Create `Step4_DocumentUpload.razor` component
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/Step4_DocumentUpload.razor`
    - Required upload fields: NationalID, ProofOfIncome, BankStatement
    - Optional upload fields: AddressProof, Other
    - Show upload status (file name, type) for each uploaded document
    - Allow remove and re-upload before submission
    - Validate all required documents uploaded before advancing (show missing list per requirement 4.5)
    - On upload: call WizardApiClient.UploadDocument (multipart)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [ ] 7.6 Create `Step5_ReviewSubmit.razor` component
    - Create `src/LoanSuperMarket.Blazor/Components/LoanApplications/Wizard/Step5_ReviewSubmit.razor`
    - Display summary: purpose, amount, term, selected product title, interest rate, uploaded documents list
    - "Submit" button: call WizardApiClient.Submit, show success, disable further edits
    - Allow navigation back to any previous step before submission
    - Show validation errors if submission fails (missing product or documents)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 8. Blazor WASM — Borrower dashboard and CRM review queue pages
  - [ ] 8.1 Create borrower dashboard page showing applications list
    - Create or enhance `src/LoanSuperMarket.Blazor/Pages/BorrowerDashboard.razor` (or integrate into existing Home.razor for Borrower role)
    - Display list of borrower's applications: ApplicationId, ProductTitle, RequestedAmount, SubmittedAtUtc, Status
    - Show "Continue Application" action for Draft applications (navigates to wizard at last step)
    - Show "Upload Documents" action for DocumentsRequested applications
    - Show document status counts (uploaded, verified, rejected) per application
    - Show matched product count for draft applications with saved amount/term
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 13.1, 13.2, 13.3_

  - [ ] 8.2 Create CRM review queue page
    - Create `src/LoanSuperMarket.Blazor/Pages/ReviewQueue.razor`
    - Display table of applications in Submitted/UnderReview/DocumentsRequested status
    - Columns: BorrowerName, RequestedAmount, ProductTitle, SubmittedAtUtc, Status
    - Support sorting by submission date, status, requested amount
    - Support filtering by status
    - Click row to navigate to application detail view
    - _Requirements: 7.1, 7.3, 7.4_

  - [ ] 8.3 Create CRM application detail/review page
    - Create `src/LoanSuperMarket.Blazor/Pages/ReviewApplicationDetail.razor`
    - Display full application details: borrower profile, credit tier, product details, all documents with verification status
    - Action buttons: Mark Under Review, Approve (with reason modal), Reject (with reason modal), Request Documents (with note modal)
    - Document actions: Verify, Reject (with note) per document
    - Show validation errors for invalid state transitions
    - _Requirements: 7.2, 8.1, 8.2, 8.3, 8.4, 9.1, 9.2, 9.3, 9.4, 10.1, 10.2_

- [ ] 9. Authorization and data isolation
  - [ ] 9.1 Configure authorization policies and resource filtering for new endpoints
    - Ensure WizardController endpoints are restricted to Borrower role
    - Ensure ReviewQueueController endpoints use CanProcessApplications policy
    - Implement IResourceFilteredQuery on GetBorrowerApplicationsQuery to filter by current borrower's UserId
    - Implement IResourceFilteredQuery on GetReviewQueueQuery (no filter for CrmManager/Admin, lender filter for Lender role)
    - Verify ownership check in wizard command handlers (borrower can only modify their own applications)
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7_

- [ ] 10. Navigation and wiring
  - [ ] 10.1 Update Blazor navigation and routing
    - Add route `/wizard` for the LoanApplicationWizard page
    - Add route `/wizard/{applicationId}` for resuming a draft
    - Add route `/review-queue` for CRM review queue page
    - Add route `/review-queue/{applicationId}` for application detail
    - Add route `/borrower/dashboard` for borrower dashboard (or enhance existing)
    - Update `NavMenu.razor` to show appropriate links based on user role (Borrower sees Wizard/Dashboard, CrmManager sees Review Queue)
    - _Requirements: 12.2, 12.3, 7.1_

## Notes

- The design uses C# throughout — all code is .NET 10 with Clean Architecture patterns
- Each task references specific requirements for traceability
- The existing `LoanApplication` entity is enhanced rather than replaced
- Document storage uses a stub implementation (local file system) ready for future cloud replacement
- The `IResourceFilteredQuery` pattern is reused for borrower/lender data isolation
- Existing `ApproveLoanApplication` and `RejectLoanApplication` commands are modified in-place to accept reason parameters

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["2.1"] },
    { "id": 3, "tasks": ["3.1", "3.2"] },
    { "id": 4, "tasks": ["3.3", "3.4", "3.5", "3.6"] },
    { "id": 5, "tasks": ["4.1"] },
    { "id": 6, "tasks": ["4.2", "4.3", "4.4", "4.5", "4.6"] },
    { "id": 7, "tasks": ["5.1", "5.2"] },
    { "id": 8, "tasks": ["6.1", "6.2"] },
    { "id": 9, "tasks": ["7.1"] },
    { "id": 10, "tasks": ["7.2", "7.3", "7.4", "7.5", "7.6"] },
    { "id": 11, "tasks": ["8.1", "8.2", "8.3"] },
    { "id": 12, "tasks": ["9.1", "10.1"] }
  ]
}
```
