# Requirements Document

## Introduction

This document defines the requirements for the Loan Application Wizard with Product Matching feature in the Loan Investment Supermarket platform. The feature enhances the existing CreateLoanApplication flow into a multi-step wizard where borrowers specify loan parameters, receive automatically matched products, select a product, upload supporting documents, and submit their application. It also introduces a CRM application review queue for CrmManagers to verify documents and approve or reject applications, along with borrower dashboard enhancements showing application status and matched products.

## Glossary

- **Application_Wizard**: The multi-step Blazor UI component that guides borrowers through the loan application process from purpose selection through submission
- **Product_Matching_Engine**: The service that filters and ranks published loan products based on a borrower's requested amount, requested term, and credit tier
- **Review_Queue**: The CRM interface where CrmManagers view submitted applications, verify documents, and approve or reject applications
- **Document_Service**: The service responsible for managing document uploads, storage references, and verification status for loan applications
- **LoanApplication**: The existing domain entity representing a borrower's loan application, enhanced with Draft and DocumentsRequested states
- **LoanProduct**: The existing domain entity representing a lender's loan offering with amount ranges, term ranges, and interest rates
- **Borrower**: The existing domain entity representing a platform borrower with a linked ApplicationUser and credit tier
- **Credit_Tier**: The classification (A, B, or C) assigned to a Borrower determining applicable interest rate ranges
- **Application_Document**: The domain entity representing a file uploaded by a borrower as part of a loan application
- **Document_Type**: The classification of an uploaded document: NationalID, ProofOfIncome, BankStatement, AddressProof, or Other
- **Document_Status**: The verification state of an uploaded document: Pending, Verified, or Rejected
- **Application_Status**: The lifecycle state of a loan application: Draft, Submitted, UnderReview, DocumentsRequested, Approved, Rejected, Funded, or Withdrawn
- **Matched_Product**: A loan product returned by the Product_Matching_Engine that meets the borrower's criteria, scored and ranked by interest rate
- **Authorization_Engine**: The existing policy-based authorization middleware enforcing access control rules

## Requirements

### Requirement 1: Wizard Step 1 — Loan Purpose and Parameters

**User Story:** As a borrower, I want to specify my loan purpose, desired amount, and term in the first step of the wizard, so that the system can find suitable loan products for me.

#### Acceptance Criteria

1. WHEN a borrower initiates a new loan application, THE Application_Wizard SHALL create a LoanApplication in Draft status and present input fields for loan purpose, requested amount, and requested term in months
2. WHEN the borrower enters a purpose exceeding 1000 characters, THE Application_Wizard SHALL display a validation error indicating the maximum length is 1000 characters
3. WHEN the borrower enters a requested amount less than or equal to zero, THE Application_Wizard SHALL display a validation error indicating the amount must be greater than zero
4. WHEN the borrower enters a requested term less than or equal to zero, THE Application_Wizard SHALL display a validation error indicating the term must be greater than zero
5. WHEN the borrower completes all required fields with valid values and proceeds, THE Application_Wizard SHALL persist the purpose, amount, and term to the Draft LoanApplication and advance to Step 2

### Requirement 2: Wizard Step 2 — Automatic Product Matching

**User Story:** As a borrower, I want to see loan products that match my criteria automatically, so that I can compare options and choose the best product for my needs.

#### Acceptance Criteria

1. WHEN the borrower advances to Step 2, THE Product_Matching_Engine SHALL retrieve all published LoanProducts where the requested amount falls within the product's MinimumAmount and MaximumAmount range (inclusive)
2. THE Product_Matching_Engine SHALL filter results to include only LoanProducts where the requested term falls within the product's MinimumTermMonths and MaximumTermMonths range (inclusive)
3. THE Product_Matching_Engine SHALL rank matched products by the effective interest rate for the borrower's Credit_Tier in ascending order (lowest rate first)
4. WHEN no published products match the borrower's criteria, THE Application_Wizard SHALL display a message indicating no matching products are available and suggest adjusting the amount or term
5. THE Application_Wizard SHALL display each Matched_Product with its title, interest rate for the borrower's tier, minimum and maximum amounts, minimum and maximum terms, and lender name
6. FOR ALL valid combinations of requested amount, requested term, and Credit_Tier, filtering then ranking SHALL produce a list sorted by ascending interest rate where every product's amount range contains the requested amount and every product's term range contains the requested term (product matching invariant)

### Requirement 3: Wizard Step 3 — Product Selection

**User Story:** As a borrower, I want to select one of the matched loan products, so that my application is linked to a specific product offering.

#### Acceptance Criteria

1. WHEN the borrower selects a Matched_Product from the list, THE Application_Wizard SHALL associate the selected LoanProduct with the Draft LoanApplication
2. WHEN the borrower attempts to proceed without selecting a product, THE Application_Wizard SHALL display a validation message indicating a product selection is required
3. WHEN the borrower selects a product and proceeds, THE Application_Wizard SHALL persist the LoanProductId to the Draft LoanApplication and advance to Step 4
4. THE Application_Wizard SHALL allow the borrower to return to Step 2 to change their selection before submission

### Requirement 4: Wizard Step 4 — Document Upload

**User Story:** As a borrower, I want to upload supporting documents as part of my application, so that the CRM team can verify my identity and financial standing.

#### Acceptance Criteria

1. THE Application_Wizard SHALL present upload fields for the following Document_Types: NationalID, ProofOfIncome, and BankStatement as required documents
2. THE Application_Wizard SHALL present an optional upload field for AddressProof and Other document types
3. WHEN the borrower uploads a document, THE Document_Service SHALL create an Application_Document entity with the file name, Document_Type, upload timestamp, and Document_Status set to Pending
4. WHEN the borrower uploads a document, THE Document_Service SHALL store the file reference using the stub storage service and associate the Application_Document with the LoanApplication
5. WHEN the borrower attempts to proceed without uploading all required document types (NationalID, ProofOfIncome, BankStatement), THE Application_Wizard SHALL display a validation message listing the missing required documents
6. THE Application_Wizard SHALL allow the borrower to remove and re-upload documents before submission
7. THE Application_Wizard SHALL display the upload status (file name and type) for each uploaded document

### Requirement 5: Wizard Step 5 — Review and Submit

**User Story:** As a borrower, I want to review all my application details before submitting, so that I can verify the information is correct.

#### Acceptance Criteria

1. THE Application_Wizard SHALL display a summary showing the loan purpose, requested amount, requested term, selected product title, selected product interest rate, and list of uploaded documents
2. WHEN the borrower confirms submission, THE Application_Wizard SHALL transition the LoanApplication from Draft to Submitted status and record the submission timestamp
3. WHEN the borrower confirms submission, THE Application_Wizard SHALL prevent further edits to the application details and documents
4. THE Application_Wizard SHALL allow the borrower to navigate back to any previous step to make changes before submission
5. IF the LoanApplication is missing a selected product or required documents at submission time, THEN THE Application_Wizard SHALL reject the submission and display the specific missing items

### Requirement 6: Application State Machine Enhancement

**User Story:** As a platform architect, I want the loan application to support a DocumentsRequested state, so that CrmManagers can request additional documents from borrowers during review.

#### Acceptance Criteria

1. THE LoanApplication SHALL support the following state transitions: Draft to Submitted, Submitted to UnderReview, UnderReview to DocumentsRequested, UnderReview to Approved, UnderReview to Rejected, DocumentsRequested to UnderReview (when borrower uploads requested documents), and Draft to Withdrawn
2. WHEN a CrmManager requests additional documents, THE LoanApplication SHALL transition from UnderReview to DocumentsRequested status
3. WHEN a borrower uploads the requested documents for an application in DocumentsRequested status, THE LoanApplication SHALL transition back to UnderReview status
4. IF a state transition is attempted that violates the allowed transitions, THEN THE LoanApplication SHALL reject the transition and return an error describing the invalid state change

### Requirement 7: CRM Application Review Queue

**User Story:** As a CrmManager, I want to view and manage submitted loan applications in a review queue, so that I can process applications efficiently.

#### Acceptance Criteria

1. WHEN a CrmManager accesses the review queue, THE Review_Queue SHALL display all LoanApplications in Submitted, UnderReview, or DocumentsRequested status with borrower name, requested amount, selected product title, submission date, and current status
2. WHEN a CrmManager selects an application, THE Review_Queue SHALL display the full application details including borrower profile, credit tier, selected product details, all uploaded documents with their verification status, and application history
3. THE Review_Queue SHALL allow sorting by submission date, status, and requested amount
4. THE Review_Queue SHALL allow filtering by application status

### Requirement 8: Document Verification by CrmManager

**User Story:** As a CrmManager, I want to verify or reject uploaded documents, so that I can confirm the borrower's identity and financial information before approving an application.

#### Acceptance Criteria

1. WHEN a CrmManager marks a document as Verified, THE Document_Service SHALL update the Document_Status to Verified and record the CrmManager's identifier and verification timestamp
2. WHEN a CrmManager marks a document as Rejected, THE Document_Service SHALL update the Document_Status to Rejected and require a rejection note explaining the reason
3. WHEN a CrmManager marks a document as Rejected, THE Document_Service SHALL record the CrmManager's identifier, rejection timestamp, and rejection note on the Application_Document
4. THE Review_Queue SHALL display the verification status of each document alongside the document file name and type

### Requirement 9: Application Approval and Rejection

**User Story:** As a CrmManager, I want to approve or reject loan applications with a mandatory reason, so that decisions are documented and auditable.

#### Acceptance Criteria

1. WHEN a CrmManager approves an application, THE Review_Queue SHALL require a mandatory reason text and transition the LoanApplication from UnderReview to Approved status
2. WHEN a CrmManager rejects an application, THE Review_Queue SHALL require a mandatory reason text and transition the LoanApplication from UnderReview to Rejected status
3. WHEN a CrmManager approves or rejects an application, THE Review_Queue SHALL record the CrmManager's identifier, decision timestamp, and reason on the LoanApplication
4. IF a CrmManager attempts to approve or reject an application that is not in UnderReview status, THEN THE Review_Queue SHALL reject the action and display an error indicating the application must be under review

### Requirement 10: Request Additional Documents

**User Story:** As a CrmManager, I want to request additional documents from a borrower, so that I can obtain missing information needed to make an approval decision.

#### Acceptance Criteria

1. WHEN a CrmManager requests additional documents, THE Review_Queue SHALL require a note describing which documents are needed and transition the LoanApplication to DocumentsRequested status
2. WHEN a LoanApplication transitions to DocumentsRequested status, THE Document_Service SHALL record the request note and CrmManager identifier on the application
3. WHILE a LoanApplication is in DocumentsRequested status, THE Application_Wizard SHALL allow the borrower to upload additional documents and re-submit for review
4. WHEN the borrower uploads documents for an application in DocumentsRequested status and confirms re-submission, THE LoanApplication SHALL transition back to UnderReview status

### Requirement 11: Document Entity and API

**User Story:** As a platform architect, I want a Document domain model and API endpoints, so that document management is consistent and accessible across the platform.

#### Acceptance Criteria

1. THE Application_Document SHALL contain the following properties: unique identifier, LoanApplication identifier, Document_Type, file name, upload timestamp, Document_Status, verified-by user identifier (nullable), verification timestamp (nullable), and rejection note (nullable)
2. WHEN a document upload request is received, THE Document_Service SHALL validate that the Document_Type is one of: NationalID, ProofOfIncome, BankStatement, AddressProof, or Other
3. WHEN a document upload request is received with an invalid Document_Type, THE Document_Service SHALL reject the request and return a validation error listing the valid types
4. THE Document_Service SHALL expose an upload endpoint that accepts the file, Document_Type, and LoanApplication identifier, stores the file reference via the stub storage service, and returns the created Application_Document identifier
5. THE Document_Service SHALL expose a query endpoint that returns all Application_Documents for a given LoanApplication identifier

### Requirement 12: Borrower Dashboard — Application Status

**User Story:** As a borrower, I want to see my active applications with their current status on my dashboard, so that I can track the progress of my loan requests.

#### Acceptance Criteria

1. WHEN a borrower accesses their dashboard, THE Application_Wizard SHALL display a list of all LoanApplications belonging to that borrower with the application identifier, selected product title, requested amount, submission date, and current Application_Status
2. WHILE a LoanApplication is in Draft status, THE Application_Wizard SHALL display a "Continue Application" action allowing the borrower to resume the wizard from where they left off
3. WHILE a LoanApplication is in DocumentsRequested status, THE Application_Wizard SHALL display an "Upload Documents" action allowing the borrower to upload the requested documents
4. THE Application_Wizard SHALL display the document upload status for each application showing the count of uploaded, verified, and rejected documents

### Requirement 13: Borrower Dashboard — Matched Products for Drafts

**User Story:** As a borrower, I want to see matched products for my draft applications, so that I can review available options before continuing the wizard.

#### Acceptance Criteria

1. WHILE a LoanApplication is in Draft status and has a saved amount and term, THE Application_Wizard SHALL display the count of matched products available for that application
2. WHEN the borrower selects a draft application to view matched products, THE Product_Matching_Engine SHALL re-execute the matching query with the saved amount, term, and borrower's current Credit_Tier
3. THE Application_Wizard SHALL indicate when no products match the draft application's criteria

### Requirement 14: Authorization and Data Isolation

**User Story:** As a platform architect, I want all wizard and review endpoints protected by appropriate authorization policies, so that data access is restricted to authorized users.

#### Acceptance Criteria

1. THE Authorization_Engine SHALL restrict loan application creation and document upload endpoints to users with the Borrower role
2. THE Authorization_Engine SHALL restrict document verification, application approval, application rejection, and document request endpoints to users with the CrmManager or Admin role
3. WHILE a user has only the Borrower role, THE Authorization_Engine SHALL restrict loan application queries to return only applications belonging to that Borrower using IResourceFilteredQuery
4. WHILE a user has only the Lender role, THE Authorization_Engine SHALL restrict loan application queries to return only applications submitted against that Lender's products using IResourceFilteredQuery
5. WHEN a Borrower attempts to access or modify a LoanApplication that does not belong to them, THE Authorization_Engine SHALL return a 403 Forbidden response
6. THE Authorization_Engine SHALL allow users with the CrmManager or Admin role to access all submitted applications without ownership restriction
7. THE Authorization_Engine SHALL enforce the CanProcessApplications policy on the review queue endpoints (mark-under-review, approve, reject, request-documents)

### Requirement 15: Product Matching Engine — Scoring and Ranking

**User Story:** As a borrower, I want matched products ranked by the best interest rate for my credit tier, so that I can easily identify the most affordable option.

#### Acceptance Criteria

1. THE Product_Matching_Engine SHALL determine the effective interest rate for each matched product based on the borrower's Credit_Tier: Tier A receives the product's base interest rate, Tier B receives the base rate plus 2 percentage points, and Tier C receives the base rate plus 4 percentage points
2. THE Product_Matching_Engine SHALL sort matched products by effective interest rate in ascending order, placing the lowest rate first
3. WHEN two or more matched products have the same effective interest rate, THE Product_Matching_Engine SHALL use the product's maximum amount as a secondary sort criterion in descending order (higher limit first)
4. THE Product_Matching_Engine SHALL return each matched product with its calculated effective interest rate, product identifier, title, amount range, term range, and lender name
5. THE Product_Matching_Engine SHALL only include LoanProducts with a Published status in matching results
