# Implementation Plan: Lender Funding & Repayment Engine

## Overview

This plan implements the Lender Funding and Repayment Engine across all Clean Architecture layers. Tasks progress from domain entities and enums, through application services and CQRS handlers, to infrastructure persistence, API controllers, and finally Blazor WASM UI pages. Each task builds incrementally on the previous, ensuring no orphaned code.

## Tasks

- [ ] 1. Domain layer — Enums, entities, and domain services
  - [x] 1.1 Create InstallmentStatus and LoanPerformance enums
    - Add `InstallmentStatus.cs` to `Domain/Enums` with values: Pending, Paid, PartiallyPaid, Late, Missed
    - Add `LoanPerformance.cs` to `Domain/Enums` with values: OnTime, Late, Defaulted
    - _Requirements: 5.2, 9.1, 10.1_

  - [x] 1.2 Add DeductFunds method to existing Lender entity
    - Add `DeductFunds(decimal amount)` method to `Lender.cs`
    - Throw `DomainException` if amount ≤ 0 or amount > AvailableFunds
    - Call `MarkUpdated()` after deduction
    - _Requirements: 2.3, 2.5, 3.1, 3.2_

  - [x] 1.3 Create Installment entity with state machine logic
    - Add `Installment.cs` to `Domain/Entities` as a sealed class extending `AuditableEntity`
    - Include all properties from design: InstallmentNumber, DueDate, PrincipalPortion, InterestPortion, TotalAmount, RemainingBalance, Status, PaidAmount, PaidDate, LateFeeAmount, Notes
    - Implement `RecordFullPayment(DateTime paymentDate)` — validates status not Paid, sets PaidAmount = TotalAmount + LateFeeAmount, Status = Paid
    - Implement `RecordPartialPayment(decimal amount, DateTime paymentDate)` — validates amount > 0, cumulative doesn't exceed total + late fee, sets Status = PartiallyPaid or Paid
    - Implement `MarkLate(decimal lateFeePercentage)` — transitions Pending/PartiallyPaid to Late, calculates late fee
    - Implement `MarkMissed()` — transitions Late to Missed
    - Guard all invalid state transitions with `DomainException`
    - _Requirements: 5.2, 6.1, 6.2, 6.3, 7.1, 8.1, 8.2, 8.3, 8.4, 8.5, 9.1, 9.3, 10.4_

  - [x] 1.4 Create RepaymentSchedule entity
    - Add `RepaymentSchedule.cs` to `Domain/Entities` as a sealed class extending `AuditableEntity`
    - Include properties: LoanApplicationId, LenderId, FundedAmount, AnnualInterestRate, TermMonths, MonthlyEmi, TotalInterestPayable, Performance, GeneratedAtUtc
    - Include private `List<Installment>` with public `IReadOnlyCollection<Installment>` accessor
    - Implement `GetNextPendingInstallment()` — returns earliest installment with status Pending, PartiallyPaid, Late, or Missed
    - Implement `GetTotalPaidToDate()` — sum of PaidAmount across all installments
    - Implement `UpdatePerformance()` — evaluates consecutive Late/Missed installments and sets Performance accordingly
    - Add navigation properties for LoanApplication and Lender
    - _Requirements: 5.1, 5.3, 5.4, 10.1, 13.2_

  - [-] 1.5 Create PaymentProcessor domain service
    - Add `IPaymentProcessor.cs` interface and `PaymentProcessor.cs` implementation in `Domain/Services` (new folder)
    - Implement `RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate)` — enforces sequential payment order (only next pending installment), delegates to installment's RecordFullPayment or RecordPartialPayment, updates schedule performance
    - Throw `DomainException` for out-of-order payments, zero/negative amounts, overpayment
    - _Requirements: 6.1, 7.2, 8.1, 8.4, 8.5, 10.5_

- [ ] 2. Application layer — AmortizationService and configuration
  - [x] 2.1 Create RepaymentSettings configuration class
    - Add `RepaymentSettings.cs` to `Application/Common/Models` (or a new `Configuration` folder)
    - Properties: GracePeriodDays (default 5), LateFeePercentage (default 0.02m), ConsecutiveMissedForDefault (default 3), UpcomingPaymentReminderDays (default 3)
    - _Requirements: 9.2, 9.3, 10.1, 11.1_

  - [-] 2.2 Create IAmortizationService interface and implementation
    - Add `IAmortizationService.cs` to `Application/Common/Interfaces`
    - Add `AmortizationService.cs` to `Application/Features/Funding` (new feature folder)
    - Implement `GenerateSchedule(Guid loanApplicationId, Guid lenderId, decimal principal, decimal annualInterestRate, int termMonths, DateTime fundingDate)` returning `RepaymentSchedule`
    - Use EMI formula: P × r × (1+r)^n / ((1+r)^n - 1) where r = annualRate / 12 / 100
    - Generate installments with correct principal/interest split, sequential due dates, and remaining balance
    - Adjust final installment principal to absorb rounding difference (round-trip invariant)
    - Validate inputs: principal > 0, rate > 0, term > 0
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_

  - [x] 2.3 Create INotificationService interface
    - Add `INotificationService.cs` to `Application/Common/Interfaces`
    - Methods: `SendPaymentReminderAsync`, `SendLatePaymentNoticeAsync`, `SendDefaultNoticeAsync`
    - _Requirements: 9.4, 10.2, 10.3, 11.1, 11.2_

- [ ] 3. Application layer — Funding feature commands and queries
  - [~] 3.1 Create FundLoanCommand and handler (replaces/enhances existing FundLoanApplicationCommand)
    - Add `FundLoanCommand.cs` and `FundLoanCommandHandler.cs` in `Application/Features/Funding/FundLoan`
    - Command takes: ApplicationId, LenderId
    - Handler: loads lender, loads loan application, calls `Lender.DeductFunds(amount)`, calls `LoanApplication.Fund()`, calculates effective rate (product rate + credit tier adjustment), calls `AmortizationService.GenerateSchedule(...)`, persists schedule, saves changes atomically
    - Returns `ApiResponse<FundingResultDto>` with schedule summary
    - _Requirements: 2.2, 2.3, 2.5, 2.6, 3.1, 3.2, 3.3, 4.1_

  - [~] 3.2 Create DeclineFundingCommand and handler
    - Add `DeclineFundingCommand.cs` and `DeclineFundingCommandHandler.cs` in `Application/Features/Funding/DeclineFunding`
    - Command takes: ApplicationId, LenderId, DeclineReason
    - Handler: validates application is Approved, records decline reason (could add a DeclineReason property or audit log), removes from queue perspective
    - Returns `ApiResponse<Unit>`
    - _Requirements: 2.4_

  - [~] 3.3 Create GetFundingQueueQuery and handler
    - Add `GetFundingQueueQuery.cs` and `GetFundingQueueQueryHandler.cs` in `Application/Features/Funding/GetFundingQueue`
    - Query implements `IResourceFilteredQuery` for lender-scoped access
    - Returns approved applications for the lender's products with borrower name, credit tier, amount, term, product title, effective rate, approval date
    - Supports filtering by product title and amount range, sorted by approval date ascending
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [~] 3.4 Create GetFundingApplicationDetailsQuery and handler
    - Add query and handler in `Application/Features/Funding/GetFundingApplicationDetails`
    - Returns full application details for the funding decision view: borrower profile, credit tier, amount, term, product details, effective rate, approval reason
    - _Requirements: 2.1_

- [ ] 4. Application layer — Payment feature commands and queries
  - [~] 4.1 Create RecordPaymentCommand and handler
    - Add `RecordPaymentCommand.cs` and `RecordPaymentCommandHandler.cs` in `Application/Features/Payments/RecordPayment`
    - Command takes: ScheduleId, Amount, PaymentDate
    - Handler: loads RepaymentSchedule with installments, delegates to `PaymentProcessor.RecordPayment(...)`, saves changes
    - Returns `ApiResponse<PaymentResultDto>`
    - _Requirements: 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 8.4, 8.5_

  - [~] 4.2 Create GetRepaymentScheduleQuery and handler
    - Add query and handler in `Application/Features/Payments/GetRepaymentSchedule`
    - Implements `IResourceFilteredQuery` for lender/borrower scoped access
    - Returns schedule with all installments, status, amounts, dates
    - _Requirements: 5.1, 5.2, 17.3, 17.4_

  - [~] 4.3 Create GetPaymentHistoryQuery and handler
    - Add query and handler in `Application/Features/Payments/GetPaymentHistory`
    - Returns payment history for a specific schedule: installment number, due date, paid date, paid amount, status
    - _Requirements: 16.1_

- [ ] 5. Application layer — Dashboard queries
  - [~] 5.1 Create GetLenderDashboardQuery and handler
    - Add query and handler in `Application/Features/Dashboard/GetLenderDashboard`
    - Implements `IResourceFilteredQuery` for lender-scoped access
    - Returns: total funded, active loan count, outstanding principal, expected monthly income, default rate, available funds
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_

  - [~] 5.2 Create GetLenderLoansQuery and handler
    - Add query and handler in `Application/Features/Dashboard/GetLenderLoans`
    - Returns list of funded loans with borrower name, funded amount, term, effective rate, LoanPerformance, next due date
    - Supports filtering by LoanPerformance and sorting by amount, due date, performance
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [~] 5.3 Create GetLenderEarningsQuery and handler
    - Add query and handler in `Application/Features/Dashboard/GetLenderEarnings`
    - Returns: total interest received, projected total returns, total late fees collected, available funds
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [~] 5.4 Create GetBorrowerLoansQuery and handler
    - Add query and handler in `Application/Features/Dashboard/GetBorrowerLoans`
    - Implements `IResourceFilteredQuery` for borrower-scoped access
    - Returns active loans with product title, funded amount, term, rate, next due date, next amount, repayment progress (paid/total count and percentage)
    - Highlights loans due within 3 days and loans with Late/Missed installments
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

  - [~] 5.5 Create GetBorrowerPaymentSummaryQuery and handler
    - Add query and handler in `Application/Features/Dashboard/GetBorrowerPaymentSummary`
    - Returns: payment history per loan, total interest paid, total principal paid, upcoming payment calendar (next 3 months)
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

- [ ] 6. Application layer — Late payment background service
  - [~] 6.1 Create LatePaymentService
    - Add `LatePaymentService.cs` in `Application/Features/Payments/LateDetection`
    - Implement `ProcessOverdueInstallmentsAsync()` — queries installments past due + grace period, marks Late, calculates late fees, sends notifications
    - Implement `ProcessMissedInstallmentsAsync()` — transitions Late installments to Missed when next due date arrives
    - Implement `DetectDefaultsAsync()` — checks for 3+ consecutive Late/Missed, updates schedule Performance to Defaulted, sends default notice
    - Implement `SendUpcomingRemindersAsync()` — sends reminders for installments due within configured days
    - Prevent duplicate notifications (track via a flag or last-notified date on installment)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 11.1, 11.2, 11.3_

- [x] 7. Shared layer — DTOs and request/response models
  - [x] 7.1 Create Funding DTOs
    - Add `Shared/Funding/FundingQueueItemDto.cs` — borrower name, credit tier, amount, term, product title, effective rate, approval date
    - Add `Shared/Funding/FundingApplicationDetailDto.cs` — full detail view
    - Add `Shared/Funding/FundingResultDto.cs` — schedule ID, EMI, total interest, term
    - Add `Shared/Funding/AcceptFundingRequest.cs` — ApplicationId
    - Add `Shared/Funding/DeclineFundingRequest.cs` — ApplicationId, Reason
    - _Requirements: 1.2, 2.1, 2.2, 2.4_

  - [x] 7.2 Create Payment DTOs
    - Add `Shared/Payments/RepaymentScheduleDto.cs` — schedule summary with installment list
    - Add `Shared/Payments/InstallmentDto.cs` — all installment fields
    - Add `Shared/Payments/PaymentResultDto.cs` — updated installment status, remaining balance
    - Add `Shared/Payments/RecordPaymentRequest.cs` — Amount, PaymentDate
    - Add `Shared/Payments/PaymentHistoryItemDto.cs` — installment number, due date, paid date, amount, status
    - _Requirements: 5.1, 5.2, 6.4, 16.1_

  - [x] 7.3 Create Dashboard DTOs
    - Add `Shared/Dashboard/LenderPortfolioDto.cs` — total funded, active loans, outstanding principal, expected income, default rate, available funds
    - Add `Shared/Dashboard/LenderLoanDto.cs` — borrower name, funded amount, term, rate, performance, next due date
    - Add `Shared/Dashboard/LenderEarningsDto.cs` — interest received, projected returns, late fees, available funds
    - Add `Shared/Dashboard/BorrowerLoanDto.cs` — product title, funded amount, term, rate, next due, next amount, progress percentage, paid/total count, highlight flags
    - Add `Shared/Dashboard/BorrowerPaymentSummaryDto.cs` — payment history, interest paid, principal paid, upcoming calendar
    - _Requirements: 12.1–12.5, 13.1–13.2, 14.1–14.4, 15.1–15.4, 16.1–16.4_

- [ ] 8. Infrastructure layer — Persistence and services
  - [-] 8.1 Create EF Core entity configurations
    - Add `RepaymentScheduleConfiguration.cs` in `Infrastructure/Persistence/Configurations`
    - Add `InstallmentConfiguration.cs` in `Infrastructure/Persistence/Configurations`
    - Configure precision, indexes, relationships as specified in design
    - Add `DbSet<RepaymentSchedule>` and `DbSet<Installment>` to `ApplicationDbContext`
    - _Requirements: 5.1, 5.2_

  - [~] 8.2 Create EF Core migration
    - Add a new migration for RepaymentSchedules and Installments tables
    - Include the Lender entity changes (no schema change needed since AvailableFunds already exists)
    - _Requirements: 5.1, 5.2_

  - [~] 8.3 Create stub NotificationService implementation
    - Add `StubNotificationService.cs` in `Infrastructure/Services`
    - Implement `INotificationService` — log notification details using `ILogger`, no actual email sending
    - _Requirements: 9.4, 10.2, 10.3, 11.1, 11.2_

  - [~] 8.4 Create LatePaymentHostedService
    - Add `LatePaymentHostedService.cs` in `Infrastructure/Services`
    - Implement `IHostedService` with a daily timer
    - Use `IServiceScopeFactory` to create scoped services
    - Delegate to `LatePaymentService` for detection logic
    - Catch exceptions per-installment, log, and continue
    - _Requirements: 9.5, 10.1_

  - [~] 8.5 Register new services in DependencyInjection
    - Register `IAmortizationService`, `IPaymentProcessor`, `INotificationService`, `LatePaymentService`
    - Register `LatePaymentHostedService` as hosted service
    - Bind `RepaymentSettings` from configuration
    - Add `RepaymentSettings` section to `appsettings.json`
    - _Requirements: 9.2, 9.5_

- [ ] 9. API layer — Controllers
  - [~] 9.1 Create FundingController
    - Add `FundingController.cs` in `Api/Controllers`
    - Endpoints: GET `/api/funding/queue`, GET `/api/funding/{applicationId}/details`, POST `/api/funding/{applicationId}/accept`, POST `/api/funding/{applicationId}/decline`
    - Apply Lender role + CanManageProducts policy authorization
    - Return `ApiResponse<T>` for all endpoints
    - _Requirements: 1.1, 2.1, 2.2, 2.4, 17.1_

  - [~] 9.2 Create PaymentsController
    - Add `PaymentsController.cs` in `Api/Controllers`
    - Endpoints: POST `/api/payments/{scheduleId}/pay`, GET `/api/payments/{scheduleId}/history`, GET `/api/payments/{scheduleId}`
    - Apply Borrower role authorization for payment recording
    - Return `ApiResponse<T>` for all endpoints
    - _Requirements: 6.1, 7.1, 16.1, 17.2_

  - [~] 9.3 Extend DashboardController with lender and borrower portfolio endpoints
    - Add endpoints to existing `DashboardController.cs`: GET `/api/dashboard/lender/portfolio`, GET `/api/dashboard/lender/loans`, GET `/api/dashboard/lender/earnings`, GET `/api/dashboard/borrower/loans`, GET `/api/dashboard/borrower/upcoming`
    - Apply appropriate role-based authorization per endpoint
    - _Requirements: 12.6, 13.1, 14.1, 15.5, 17.3, 17.4, 17.6_

- [ ] 10. Blazor WASM — API clients and services
  - [~] 10.1 Create FundingApiClient
    - Add `FundingApiClient.cs` in `Blazor/Services/ApiClients`
    - Methods: `GetFundingQueueAsync`, `GetApplicationDetailsAsync`, `AcceptFundingAsync`, `DeclineFundingAsync`
    - Use existing HttpClient and `ApiResponse<T>` deserialization patterns
    - _Requirements: 1.1, 2.1, 2.2, 2.4_

  - [~] 10.2 Create PaymentsApiClient
    - Add `PaymentsApiClient.cs` in `Blazor/Services/ApiClients`
    - Methods: `RecordPaymentAsync`, `GetRepaymentScheduleAsync`, `GetPaymentHistoryAsync`
    - _Requirements: 6.1, 16.1_

  - [~] 10.3 Create DashboardApiClient extensions for lender/borrower portfolio
    - Extend existing dashboard API client (or create new methods) for: `GetLenderPortfolioAsync`, `GetLenderLoansAsync`, `GetLenderEarningsAsync`, `GetBorrowerLoansAsync`, `GetBorrowerPaymentSummaryAsync`
    - _Requirements: 12.1, 13.1, 14.1, 15.1, 16.1_

- [ ] 11. Blazor WASM — Funding Queue UI
  - [~] 11.1 Create FundingQueue page
    - Add `FundingQueue.razor` in `Blazor/Pages`
    - Display data grid of approved applications with columns: borrower name, credit tier, amount, term, product, effective rate, approval date
    - Add filter controls for product title and amount range
    - Sort by approval date ascending by default
    - Show "no remaining capital" notification when lender funds are zero
    - Include "View Details" action button per row
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 3.4_

  - [~] 11.2 Create FundingDecision component
    - Add `FundingDecision.razor` in `Blazor/Components/Funding` (new folder)
    - Display full application details: borrower profile, credit tier, amount, term, product, effective rate, approval reason
    - Include "Accept Funding" and "Decline Funding" buttons
    - Show confirmation modal before accepting
    - Show decline reason text input before declining
    - Display error messages for insufficient funds or invalid status
    - On success, show funding result (EMI, total interest, schedule summary) and refresh queue
    - _Requirements: 2.1, 2.2, 2.4, 2.5, 2.6_

- [ ] 12. Blazor WASM — Payment UI
  - [~] 12.1 Create RepaymentSchedule page
    - Add `RepaymentSchedule.razor` in `Blazor/Pages`
    - Display schedule summary: funded amount, rate, term, EMI, total interest
    - Display installment table: number, due date, principal, interest, total, remaining balance, status, paid amount, paid date, late fee
    - Highlight next pending installment
    - Include "Make Payment" button on the next pending installment row
    - _Requirements: 5.1, 5.2, 6.4_

  - [~] 12.2 Create PaymentForm component
    - Add `PaymentForm.razor` in `Blazor/Components/Payments` (new folder)
    - Show installment details: number, due date, total amount, remaining to pay (total + late fee - paid)
    - Input field for payment amount with validation (> 0, ≤ remaining)
    - "Pay Full Amount" quick-fill button and "Pay" submit button
    - Display success/error feedback after submission
    - _Requirements: 6.1, 8.1, 8.4, 8.5_

- [ ] 13. Blazor WASM — Lender Dashboard enhancements
  - [~] 13.1 Create LenderPortfolio component
    - Add `LenderPortfolio.razor` in `Blazor/Components/Dashboard`
    - Display summary cards: total funded, active loans, outstanding principal, expected monthly income, default rate, available funds
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

  - [~] 13.2 Create LenderLoans component
    - Add `LenderLoans.razor` in `Blazor/Components/Dashboard`
    - Display data grid of funded loans: borrower name, funded amount, term, rate, performance badge (OnTime/Late/Defaulted with color coding), next due date
    - Filter by LoanPerformance status
    - Sort by funded amount, next due date, performance
    - Link each row to the repayment schedule detail page
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [~] 13.3 Create LenderEarnings component
    - Add `LenderEarnings.razor` in `Blazor/Components/Dashboard`
    - Display: total interest received, projected total returns, late fees collected, available funds balance
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 14. Blazor WASM — Borrower Dashboard enhancements
  - [~] 14.1 Create BorrowerLoans component
    - Add `BorrowerLoans.razor` in `Blazor/Components/Dashboard`
    - Display active loans: product title, funded amount, term, rate, next due date, next amount, progress bar (paid/total)
    - Highlight loans due within 3 days (warning style)
    - Highlight loans with Late/Missed installments (danger style)
    - Link each loan to its repayment schedule page
    - _Requirements: 15.1, 15.2, 15.3, 15.4_

  - [~] 14.2 Create BorrowerPaymentHistory component
    - Add `BorrowerPaymentHistory.razor` in `Blazor/Components/Dashboard`
    - Display payment history table per loan: installment number, due date, paid date, paid amount, status
    - Display summary: total interest paid, total principal paid
    - _Requirements: 16.1, 16.2, 16.3_

  - [~] 14.3 Create UpcomingPayments component
    - Add `UpcomingPayments.razor` in `Blazor/Components/Dashboard`
    - Display upcoming payment calendar for next 3 months across all active loans
    - Show due date, amount, and loan product title for each upcoming installment
    - _Requirements: 16.4_

- [ ] 15. Wire Blazor pages into navigation and integrate dashboard
  - [~] 15.1 Update NavMenu and routing
    - Add "Funding Queue" link visible to Lender role in `NavMenu.razor`
    - Add navigation from lender/borrower dashboard pages to new components
    - Ensure route parameters are configured for schedule detail pages
    - Register new API clients in Blazor `Program.cs` DI container
    - _Requirements: 1.1, 12.6, 15.5_

  - [~] 15.2 Integrate lender dashboard components into LenderDashboard page
    - Wire `LenderPortfolio`, `LenderLoans`, and `LenderEarnings` components into the lender dashboard page (create or extend existing page)
    - Add tab or section navigation between portfolio, loans, and earnings views
    - _Requirements: 12.1, 13.1, 14.1_

  - [~] 15.3 Integrate borrower dashboard components into BorrowerDashboard page
    - Wire `BorrowerLoans`, `BorrowerPaymentHistory`, and `UpcomingPayments` components into the existing `BorrowerDashboard.razor` page
    - Add tab or section navigation between active loans, payment history, and upcoming payments
    - _Requirements: 15.1, 16.1, 16.4_

## Notes

- Each task references specific requirements for traceability
- The existing `FundLoanApplicationCommand` in `Features/LoanApplications/FundLoanApplication` should be deprecated or redirected to the new `FundLoanCommand` in `Features/Funding` which adds capital deduction and schedule generation
- Use the existing `ApiResponse<T>` pattern from `Shared/Common/ApiResponse.cs` for all API responses
- Use `IResourceFilteredQuery` on queries that need lender/borrower data isolation
- The stub notification service logs messages only — no real email infrastructure needed
- Credit tier rate adjustment: A = base rate, B = base + 2%, C = base + 4% — calculated once at funding time and stored on RepaymentSchedule

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "2.1", "2.3"] },
    { "id": 1, "tasks": ["1.3", "1.4", "7.1", "7.2", "7.3"] },
    { "id": 2, "tasks": ["1.5", "2.2", "8.1"] },
    { "id": 3, "tasks": ["3.1", "3.2", "3.3", "3.4", "8.2"] },
    { "id": 4, "tasks": ["4.1", "4.2", "4.3", "6.1"] },
    { "id": 5, "tasks": ["5.1", "5.2", "5.3", "5.4", "5.5", "8.3"] },
    { "id": 6, "tasks": ["8.4", "8.5", "9.1", "9.2", "9.3"] },
    { "id": 7, "tasks": ["10.1", "10.2", "10.3"] },
    { "id": 8, "tasks": ["11.1", "11.2", "12.1", "12.2"] },
    { "id": 9, "tasks": ["13.1", "13.2", "13.3", "14.1", "14.2", "14.3"] },
    { "id": 10, "tasks": ["15.1", "15.2", "15.3"] }
  ]
}
```
