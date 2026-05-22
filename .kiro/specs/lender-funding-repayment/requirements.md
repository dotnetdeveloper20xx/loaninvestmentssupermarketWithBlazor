# Requirements Document

## Introduction

This document defines the requirements for the Lender Funding and Repayment Engine feature in the Loan Investment Supermarket platform. The feature enables lenders to fund approved loan applications, generates amortization schedules for funded loans, processes borrower payments (on-time, early, partial, and late), enforces late payment and default handling rules, and provides portfolio and loan dashboards for lenders and borrowers respectively.

## Glossary

- **Funding_Engine**: The service responsible for processing lender funding decisions on approved loan applications, enforcing capital limits, and recording disbursements
- **Funding_Queue**: The lender-facing UI component that displays approved loan applications matched against the lender's products, allowing the lender to accept or decline funding
- **Amortization_Service**: The service that generates a full repayment schedule using the standard Equal Monthly Installment (EMI) formula when a loan is funded
- **Repayment_Schedule**: The domain entity representing the complete set of monthly installments for a funded loan, including principal, interest, and balance breakdown
- **Installment**: The domain entity representing a single monthly payment within a Repayment_Schedule, containing due date, principal portion, interest portion, total amount, remaining balance, and payment status
- **Payment_Processor**: The service responsible for recording borrower payments against installments, handling on-time, early, partial, and late payment scenarios
- **Late_Payment_Service**: The service that detects overdue installments, applies grace periods, calculates late fees, and flags defaulted loans
- **Lender_Dashboard**: The UI component displaying a lender's portfolio metrics including total funded, active loans, outstanding principal, expected income, default rate, and earnings
- **Borrower_Dashboard**: The UI component displaying a borrower's active loans, repayment progress, payment history, and upcoming payments
- **LoanApplication**: The existing domain entity representing a borrower's loan application with states including Approved and Funded
- **Lender**: The existing domain entity representing a platform lender with AvailableFunds property
- **Borrower**: The existing domain entity representing a platform borrower with a linked ApplicationUser and Credit_Tier
- **LoanProduct**: The existing domain entity representing a lender's loan offering with interest rates and term ranges
- **Credit_Tier**: The classification (A, B, or C) assigned to a Borrower determining applicable interest rate adjustments (A=base, B=base+2%, C=base+4%)
- **EMI**: Equal Monthly Installment calculated using the standard amortization formula: EMI = P × r × (1+r)^n / ((1+r)^n - 1), where P is principal, r is monthly interest rate, and n is number of months
- **Grace_Period**: A configurable number of days after an installment due date before the installment is flagged as late (default: 5 days)
- **Late_Fee**: A configurable percentage of the overdue installment amount charged when payment is not received within the Grace_Period
- **Default_Status**: A loan status applied when 3 or more consecutive installments are missed, triggering CustomerService attention
- **Installment_Status**: The payment state of an installment: Pending, Paid, PartiallyPaid, Late, or Missed
- **Loan_Performance**: The classification of a funded loan's payment behavior: OnTime, Late, or Defaulted
- **Authorization_Engine**: The existing policy-based authorization middleware enforcing access control rules

## Requirements

### Requirement 1: Funding Queue Display

**User Story:** As a lender, I want to see approved loan applications matched against my products in a funding queue, so that I can decide which loans to fund.

#### Acceptance Criteria

1. WHEN a lender accesses the Funding_Queue, THE Funding_Queue SHALL display all LoanApplications in Approved status that are associated with LoanProducts owned by that lender
2. THE Funding_Queue SHALL display each application with the borrower's full name, Credit_Tier, requested amount, term in months, selected product title, effective interest rate, and approval date
3. THE Funding_Queue SHALL sort applications by approval date in ascending order (oldest first)
4. THE Funding_Queue SHALL allow filtering by product title and requested amount range
5. THE Authorization_Engine SHALL restrict the Funding_Queue to display only applications against the authenticated lender's own products using IResourceFilteredQuery

### Requirement 2: Lender Funding Decision

**User Story:** As a lender, I want to accept or decline funding for approved applications, so that I can control which loans I invest in.

#### Acceptance Criteria

1. WHEN a lender selects an application from the Funding_Queue, THE Funding_Queue SHALL display the full application details including borrower profile, Credit_Tier, requested amount, term, product details, effective interest rate, and approval reason
2. WHEN a lender accepts funding for an application, THE Funding_Engine SHALL transition the LoanApplication from Approved to Funded status using the existing Fund() method
3. WHEN a lender accepts funding, THE Funding_Engine SHALL reduce the lender's AvailableFunds by the funded amount
4. WHEN a lender declines funding for an application, THE Funding_Engine SHALL record the decline reason and remove the application from that lender's Funding_Queue
5. IF a lender attempts to fund an application where the requested amount exceeds the lender's AvailableFunds, THEN THE Funding_Engine SHALL reject the funding and display an error indicating insufficient available funds
6. IF a lender attempts to fund an application that is not in Approved status, THEN THE Funding_Engine SHALL reject the action and display an error indicating only approved applications can be funded

### Requirement 3: Capital Limit Enforcement

**User Story:** As a platform architect, I want the system to enforce capital limits when lenders fund loans, so that lenders cannot overcommit their available capital.

#### Acceptance Criteria

1. WHEN a lender accepts funding, THE Funding_Engine SHALL validate that the lender's AvailableFunds is greater than or equal to the requested loan amount before processing
2. WHEN funding is successfully processed, THE Funding_Engine SHALL atomically deduct the funded amount from the lender's AvailableFunds and persist the updated balance
3. THE Funding_Engine SHALL record the funding transaction with the lender identifier, application identifier, funded amount, and funding timestamp
4. IF the lender's AvailableFunds becomes zero after funding, THEN THE Funding_Queue SHALL display a notification indicating the lender has no remaining capital available

### Requirement 4: Amortization Schedule Generation

**User Story:** As a platform architect, I want the system to generate a full amortization schedule when a loan is funded, so that both lender and borrower have a clear repayment plan.

#### Acceptance Criteria

1. WHEN a LoanApplication transitions to Funded status, THE Amortization_Service SHALL generate a Repayment_Schedule containing one Installment for each month of the loan term
2. THE Amortization_Service SHALL calculate the EMI using the standard amortization formula: EMI = P × r × (1+r)^n / ((1+r)^n - 1), where P is the funded principal amount, r is the monthly interest rate (annual effective rate divided by 12), and n is the term in months
3. THE Amortization_Service SHALL calculate each Installment's principal portion and interest portion such that the sum of all principal portions equals the original funded amount (within rounding tolerance of 0.01 currency units)
4. THE Amortization_Service SHALL set each Installment's due date to the same day of the month, starting one month after the funding date
5. THE Amortization_Service SHALL set each Installment's remaining balance to the principal outstanding after that installment's principal portion is applied
6. THE Amortization_Service SHALL set the initial Installment_Status of each installment to Pending
7. FOR ALL valid combinations of principal amount, annual interest rate, and term months, generating a schedule then summing all principal portions SHALL equal the original principal amount within 0.01 currency units (amortization round-trip invariant)
8. FOR ALL generated schedules, each Installment's interest portion SHALL equal the remaining balance before that installment multiplied by the monthly rate, rounded to 2 decimal places (interest calculation invariant)

### Requirement 5: Repayment Schedule Entity

**User Story:** As a platform architect, I want a Repayment Schedule domain model, so that repayment data is structured and queryable.

#### Acceptance Criteria

1. THE Repayment_Schedule SHALL contain the following properties: unique identifier, LoanApplication identifier, lender identifier, funded amount, annual interest rate, term in months, monthly EMI amount, total interest payable, generation timestamp, and a collection of Installments
2. THE Installment SHALL contain the following properties: unique identifier, Repayment_Schedule identifier, installment number (1-based sequential), due date, principal portion, interest portion, total amount (principal + interest), remaining balance after payment, Installment_Status, paid amount, paid date (nullable), late fee amount (default 0), and notes (nullable)
3. THE Repayment_Schedule SHALL expose a method to retrieve the next pending installment by due date
4. THE Repayment_Schedule SHALL expose a method to calculate total paid to date (sum of paid amounts across all installments)

### Requirement 6: Borrower Payment Processing — On-Time Payment

**User Story:** As a borrower, I want to make payments on my loan installments, so that I can fulfill my repayment obligations.

#### Acceptance Criteria

1. WHEN a borrower makes a full payment on a Pending installment on or before the due date, THE Payment_Processor SHALL update the Installment_Status to Paid, record the paid amount equal to the total installment amount, and record the payment date
2. WHEN a payment is recorded, THE Payment_Processor SHALL validate that the payment amount matches the installment total amount for a full payment
3. THE Payment_Processor SHALL prevent payments on installments that are already in Paid status
4. WHEN a payment is successfully recorded, THE Borrower_Dashboard SHALL reflect the updated repayment progress

### Requirement 7: Borrower Payment Processing — Early Payment

**User Story:** As a borrower, I want to make early payments on future installments, so that I can reduce my outstanding balance ahead of schedule.

#### Acceptance Criteria

1. WHEN a borrower makes a payment on an installment before its due date, THE Payment_Processor SHALL process the payment identically to an on-time payment and update the Installment_Status to Paid
2. THE Payment_Processor SHALL allow payment on the next pending installment only (sequential payment order enforcement)
3. THE Payment_Processor SHALL record the actual payment date regardless of the installment's scheduled due date

### Requirement 8: Borrower Payment Processing — Partial Payment

**User Story:** As a borrower, I want to make partial payments when I cannot afford the full installment, so that I can reduce my outstanding amount incrementally.

#### Acceptance Criteria

1. WHEN a borrower makes a payment less than the full installment amount, THE Payment_Processor SHALL update the Installment_Status to PartiallyPaid and record the paid amount
2. WHEN a subsequent payment is made on a PartiallyPaid installment, THE Payment_Processor SHALL add the new payment to the existing paid amount
3. WHEN the cumulative paid amount on a PartiallyPaid installment equals or exceeds the total installment amount, THE Payment_Processor SHALL update the Installment_Status to Paid
4. THE Payment_Processor SHALL reject payment amounts that are less than or equal to zero
5. THE Payment_Processor SHALL reject payment amounts that would cause the cumulative paid amount to exceed the total installment amount plus any applicable late fee

### Requirement 9: Late Payment Detection

**User Story:** As a platform architect, I want the system to automatically detect late payments, so that overdue installments are flagged for follow-up.

#### Acceptance Criteria

1. WHEN an installment's due date plus the configured Grace_Period has passed and the Installment_Status is Pending or PartiallyPaid, THE Late_Payment_Service SHALL update the Installment_Status to Late
2. THE Late_Payment_Service SHALL use a configurable Grace_Period defaulting to 5 days
3. WHEN an installment is marked as Late, THE Late_Payment_Service SHALL calculate a late fee as a configurable percentage (default: 2%) of the overdue installment total amount and record the late fee on the Installment
4. WHEN an installment is marked as Late, THE Late_Payment_Service SHALL send a notification to the borrower indicating the overdue amount and applicable late fee (stub email implementation)
5. THE Late_Payment_Service SHALL execute the late payment detection check on a scheduled basis (daily)

### Requirement 10: Default Handling

**User Story:** As a platform architect, I want loans with multiple consecutive missed payments to be flagged as defaulted, so that they receive appropriate attention from CustomerService.

#### Acceptance Criteria

1. WHEN a loan has 3 or more consecutive installments with Installment_Status of Late or Missed, THE Late_Payment_Service SHALL mark the loan's Loan_Performance as Defaulted
2. WHEN a loan is marked as Defaulted, THE Late_Payment_Service SHALL flag the loan for CustomerService attention by creating a notification record
3. WHEN a loan is marked as Defaulted, THE Late_Payment_Service SHALL send a notification to the borrower indicating the default status and advising them to contact CustomerService (stub email implementation)
4. THE Late_Payment_Service SHALL update an installment from Late to Missed when the next installment's due date arrives and the Late installment remains unpaid
5. WHILE a loan is in Defaulted status, THE Payment_Processor SHALL still accept payments on outstanding installments

### Requirement 11: Borrower Upcoming Payment Notifications

**User Story:** As a borrower, I want to receive notifications about upcoming payments, so that I can prepare funds and avoid late fees.

#### Acceptance Criteria

1. WHEN an installment due date is 3 days away and the Installment_Status is Pending, THE Late_Payment_Service SHALL send a reminder notification to the borrower with the due date and amount (stub email implementation)
2. WHEN an installment due date has passed and the Installment_Status is Late, THE Late_Payment_Service SHALL send an overdue notification to the borrower with the overdue amount and late fee (stub email implementation)
3. THE Late_Payment_Service SHALL not send duplicate notifications for the same installment event

### Requirement 12: Lender Portfolio Dashboard — Summary Metrics

**User Story:** As a lender, I want to see a summary of my lending portfolio, so that I can monitor my investment performance at a glance.

#### Acceptance Criteria

1. WHEN a lender accesses the Lender_Dashboard, THE Lender_Dashboard SHALL display the total amount funded across all loans by that lender
2. THE Lender_Dashboard SHALL display the count of active loans (funded loans that are not fully repaid and not defaulted)
3. THE Lender_Dashboard SHALL display the total outstanding principal (sum of remaining balance across all active loan installments that are not yet Paid)
4. THE Lender_Dashboard SHALL display the expected monthly income calculated as the sum of interest portions from the next pending installment across all active loans
5. THE Lender_Dashboard SHALL display the default rate calculated as the count of Defaulted loans divided by the total count of funded loans, expressed as a percentage
6. THE Authorization_Engine SHALL restrict the Lender_Dashboard to display only data for loans funded by the authenticated lender

### Requirement 13: Lender Portfolio Dashboard — Individual Loan Performance

**User Story:** As a lender, I want to see the performance of each funded loan, so that I can identify loans that need attention.

#### Acceptance Criteria

1. THE Lender_Dashboard SHALL display a list of all funded loans with borrower name, funded amount, term, effective interest rate, Loan_Performance status, and next payment due date
2. THE Lender_Dashboard SHALL classify each loan's Loan_Performance as OnTime when all due installments are Paid, Late when any due installment has Late or Missed status but fewer than 3 consecutive, and Defaulted when 3 or more consecutive installments are Late or Missed
3. THE Lender_Dashboard SHALL allow filtering loans by Loan_Performance status
4. THE Lender_Dashboard SHALL allow sorting loans by funded amount, next due date, and Loan_Performance status

### Requirement 14: Lender Portfolio Dashboard — Earnings Tracker

**User Story:** As a lender, I want to track my earnings from interest payments, so that I can measure my return on investment.

#### Acceptance Criteria

1. THE Lender_Dashboard SHALL display the total interest received to date (sum of interest portions from all Paid installments across the lender's funded loans)
2. THE Lender_Dashboard SHALL display the projected total returns (sum of all interest portions across all installments in all active Repayment_Schedules for the lender)
3. THE Lender_Dashboard SHALL display the total late fees collected (sum of late fee amounts from all Paid installments that had a late fee applied)
4. THE Lender_Dashboard SHALL display the current available funds balance for the lender

### Requirement 15: Borrower Loan Dashboard — Active Loans

**User Story:** As a borrower, I want to see my active loans with upcoming payment details, so that I can manage my repayment obligations.

#### Acceptance Criteria

1. WHEN a borrower accesses the Borrower_Dashboard, THE Borrower_Dashboard SHALL display all funded loans belonging to that borrower with the product title, funded amount, term, effective interest rate, next payment due date, and next payment amount
2. THE Borrower_Dashboard SHALL display the repayment progress for each loan as the count of Paid installments versus total installments and as a percentage
3. THE Borrower_Dashboard SHALL highlight loans where the next installment is due within 3 days
4. THE Borrower_Dashboard SHALL highlight loans where any installment has Late or Missed status
5. THE Authorization_Engine SHALL restrict the Borrower_Dashboard to display only loans belonging to the authenticated borrower

### Requirement 16: Borrower Loan Dashboard — Payment History and Summary

**User Story:** As a borrower, I want to see my payment history and total interest paid, so that I can track my financial obligations over time.

#### Acceptance Criteria

1. THE Borrower_Dashboard SHALL display a payment history list for each loan showing installment number, due date, paid date, paid amount, and Installment_Status
2. THE Borrower_Dashboard SHALL display the total interest paid to date for each loan (sum of interest portions from Paid installments)
3. THE Borrower_Dashboard SHALL display the total principal paid to date for each loan (sum of principal portions from Paid installments)
4. THE Borrower_Dashboard SHALL display an upcoming payment calendar showing all Pending installment due dates and amounts across all active loans for the next 3 months

### Requirement 17: Authorization and Data Isolation

**User Story:** As a platform architect, I want all funding and repayment endpoints protected by appropriate authorization policies, so that data access is restricted to authorized users.

#### Acceptance Criteria

1. THE Authorization_Engine SHALL restrict funding acceptance and decline endpoints to users with the Lender role and the CanManageProducts policy
2. THE Authorization_Engine SHALL restrict payment recording endpoints to users with the Borrower role
3. WHILE a user has only the Lender role, THE Authorization_Engine SHALL restrict Repayment_Schedule queries to return only schedules for loans funded by that lender using IResourceFilteredQuery
4. WHILE a user has only the Borrower role, THE Authorization_Engine SHALL restrict Repayment_Schedule queries to return only schedules for loans belonging to that borrower using IResourceFilteredQuery
5. THE Authorization_Engine SHALL restrict late payment detection and default handling operations to system-level scheduled processes (no direct user access)
6. THE Authorization_Engine SHALL allow users with the Admin role to access all funding and repayment data without ownership restriction
