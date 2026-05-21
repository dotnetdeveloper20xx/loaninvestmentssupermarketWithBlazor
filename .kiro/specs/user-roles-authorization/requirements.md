# Requirements Document

## Introduction

This document defines the requirements for implementing User Roles and Authorization in the Loan Investment Supermarket platform. The feature introduces ASP.NET Identity with JWT-based authentication, role-based access control with policy-based authorization, and Blazor WebAssembly integration for a secure, role-appropriate user experience across the financial services platform.

## Glossary

- **Auth_System**: The authentication and authorization subsystem responsible for user identity management, token issuance, and access control enforcement
- **Token_Service**: The service responsible for generating, validating, and refreshing JWT access tokens and refresh tokens
- **Identity_Store**: The ASP.NET Identity-backed persistence layer storing user accounts, roles, claims, and security metadata
- **Authorization_Engine**: The policy-based authorization middleware that evaluates access control rules against user claims and roles
- **Auth_State_Provider**: The Blazor WebAssembly custom AuthenticationStateProvider that reads JWT claims and exposes authentication state to the UI
- **User_Management_Module**: The administrative interface for creating, editing, enabling, disabling, and assigning roles to user accounts
- **Audit_Logger**: The service that records authentication and authorization events to the AuditLog entity for compliance tracking
- **ApplicationUser**: The ASP.NET Identity user entity extending IdentityUser with platform-specific properties
- **Access_Token**: A short-lived JWT (15 minutes) containing user claims, roles, and permissions used to authorize API requests
- **Refresh_Token**: A long-lived token (7 days) used to obtain new access tokens without re-authentication
- **Role**: A named grouping (Admin, CrmManager, CustomerService, Lender, Borrower, Auditor) that determines a user's permissions within the platform
- **Policy**: A named authorization rule (e.g., CanManageUsers, CanVetUsers, CanHandleDisputes) that maps one or more roles to specific access permissions
- **Resource_Owner**: The user who owns a specific entity (e.g., a Borrower owns their loan applications, a Lender owns their loan products)
- **CrmManager**: The role responsible for user vetting, registration approval/rejection, loan product approval, credit tier assignment, account limit management, and AML compliance checks
- **CustomerService**: The role responsible for handling user disputes, messaging moderation, FAQ management, late payment case handling, and mediation between borrowers and lenders
- **Account_Status**: The current state of a user account, one of: Active, Hold, Blocked, Suspended, or Closed
- **Credit_Tier**: A classification (A, B, or C) assigned to a Borrower based on financial profile, determining interest rate ranges and loan limits
- **Vetting_Workflow**: The process by which new user registrations are reviewed and approved or rejected by a CrmManager before platform access is granted
- **Permission_Module**: A logical grouping of permissions by platform area (Users, Loans, Products, Finance, Reports, Settings, Messages) with granular action-level control
- **Two_Factor_Auth**: The TOTP-based two-factor authentication mechanism providing an additional security layer beyond password authentication
- **Session_Manager**: The service responsible for tracking, managing, and terminating active user sessions across devices
- **Custom_Role**: A user-defined role created by an Admin with a custom set of granular permissions, extending beyond the predefined role set
- **Recovery_Code**: A one-time-use backup code provided when Two_Factor_Auth is enabled, allowing account access if the TOTP device is unavailable

## Requirements

### Requirement 1: User Registration

**User Story:** As a new platform user, I want to register an account with my email and password, so that I can access the platform with appropriate permissions.

#### Acceptance Criteria

1. WHEN a registration request is received with a valid email and password, THE Auth_System SHALL create a new ApplicationUser in the Identity_Store and return a success response with the user identifier
2. WHEN a registration request is received with an email that already exists in the Identity_Store, THE Auth_System SHALL reject the request and return an error indicating the email is already registered
3. WHEN a registration request is received with a password that does not meet complexity requirements, THE Auth_System SHALL reject the request and return specific validation errors describing which rules were violated
4. THE Auth_System SHALL enforce password complexity requiring a minimum of 8 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
5. WHEN a Borrower registers, THE Auth_System SHALL assign the Borrower role to the new account and set the Account_Status to "Pending Approval"
6. WHEN a Lender registers, THE Auth_System SHALL assign the Lender role to the new account and set the Account_Status to "Pending Approval"
7. WHEN a registration request is received with an invalid email format, THE Auth_System SHALL reject the request and return a validation error indicating the email format is invalid
8. WHEN a registration is completed, THE Auth_System SHALL send an email verification link to the registered email address
9. WHEN the email verification link is confirmed, THE Auth_System SHALL mark the email as verified and make the account eligible for CrmManager review
10. THE Auth_System SHALL provide a "Remember Me" option during login that extends the Refresh_Token expiration to 30 days
11. THE Auth_System SHALL present different registration flows for Borrower and Lender roles, collecting role-specific information during registration

### Requirement 2: User Authentication (Login)

**User Story:** As a registered user, I want to log in with my email and password, so that I can receive a JWT token to access protected resources.

#### Acceptance Criteria

1. WHEN a login request is received with valid credentials, THE Token_Service SHALL issue an Access_Token (JWT) and a Refresh_Token, returning both to the client
2. THE Token_Service SHALL include the user identifier, email, roles, and policy claims in the Access_Token payload
3. THE Token_Service SHALL set the Access_Token expiration to 15 minutes from issuance
4. THE Token_Service SHALL set the Refresh_Token expiration to 7 days from issuance
5. WHEN a login request is received with an invalid email or password, THE Auth_System SHALL return a generic authentication failure message without revealing which credential was incorrect
6. WHEN a login request is received for a disabled account, THE Auth_System SHALL reject the request and return an error indicating the account is disabled
7. WHEN a successful login occurs, THE Audit_Logger SHALL record the login event including the user identifier, timestamp, and IP address

### Requirement 3: Account Lockout

**User Story:** As a platform administrator, I want accounts to be locked after repeated failed login attempts, so that brute-force attacks are mitigated.

#### Acceptance Criteria

1. WHEN a login attempt fails, THE Auth_System SHALL increment the failed login attempt counter for the targeted account
2. WHEN the failed login attempt counter reaches 5 for an account, THE Auth_System SHALL lock the account for 15 minutes
3. WHILE an account is locked, THE Auth_System SHALL reject all login attempts for that account and return an error indicating the account is temporarily locked
4. WHEN the lockout duration expires, THE Auth_System SHALL reset the failed login attempt counter and allow login attempts to proceed
5. WHEN a successful login occurs, THE Auth_System SHALL reset the failed login attempt counter to zero
6. WHEN an account is locked, THE Audit_Logger SHALL record the lockout event including the user identifier and lockout expiration time

### Requirement 4: Token Refresh

**User Story:** As an authenticated user, I want my session to be extended seamlessly using refresh tokens, so that I am not forced to re-enter credentials frequently.

#### Acceptance Criteria

1. WHEN a valid Refresh_Token is presented to the token refresh endpoint, THE Token_Service SHALL issue a new Access_Token and a new Refresh_Token, invalidating the previous Refresh_Token
2. WHEN an expired Refresh_Token is presented, THE Token_Service SHALL reject the request and return an error indicating the session has expired
3. WHEN a Refresh_Token that has been previously invalidated is presented, THE Token_Service SHALL reject the request, invalidate all Refresh_Tokens for that user, and return an error indicating potential token reuse
4. THE Token_Service SHALL store Refresh_Tokens in the Identity_Store with their expiration date, creation timestamp, and associated user identifier
5. WHEN a new Access_Token is issued via refresh, THE Token_Service SHALL include the same claims and roles as the original token, reflecting any role changes made since the last issuance

### Requirement 5: Password Reset

**User Story:** As a user who has forgotten my password, I want to reset it via email verification, so that I can regain access to my account.

#### Acceptance Criteria

1. WHEN a password reset request is received with a registered email, THE Auth_System SHALL generate a time-limited reset token and send it to the registered email address
2. WHEN a password reset request is received with an unregistered email, THE Auth_System SHALL return a success response without sending an email to prevent email enumeration
3. WHEN a valid reset token and new password are submitted, THE Auth_System SHALL update the user password, invalidate all existing Refresh_Tokens for that user, and return a success response
4. WHEN an expired or invalid reset token is submitted, THE Auth_System SHALL reject the request and return an error indicating the token is invalid or expired
5. THE Auth_System SHALL set the password reset token expiration to 1 hour from generation
6. WHEN a password is successfully reset, THE Audit_Logger SHALL record the password reset event including the user identifier and timestamp

### Requirement 6: Role Definition and Seeding

**User Story:** As a platform administrator, I want predefined roles available on first deployment, so that the system is ready for user assignment without manual configuration.

#### Acceptance Criteria

1. THE Auth_System SHALL define the following roles in the Identity_Store: Admin, CrmManager, CustomerService, Lender, Borrower, and Auditor
2. WHEN the application starts and the roles do not exist in the Identity_Store, THE Auth_System SHALL create all predefined roles automatically
3. WHEN the application starts and no Admin user exists, THE Auth_System SHALL create a default Admin account with a configurable email and password from application settings
4. THE Auth_System SHALL associate each role with its corresponding authorization policies: Admin with all policies, CrmManager with CanVetUsers, CanApproveProducts, CanSetLimits, and CanManageBorrowers, CustomerService with CanHandleDisputes and CanManageMessages, Lender with CanManageProducts, Borrower with no administrative policies, and Auditor with CanViewReports
5. THE Auth_System SHALL define the Admin role as having full platform access including user management, system configuration, and final loan disbursement approval
6. THE Auth_System SHALL define the CrmManager role as responsible for user vetting, registration approval/rejection, loan product approval, credit tier assignment, account limit management, and AML compliance
7. THE Auth_System SHALL define the CustomerService role as responsible for handling disputes, messaging moderation, FAQ management, late payment cases, and mediation between borrowers and lenders
8. THE Auth_System SHALL define the Lender role as able to create loan products, view own products, and see applications submitted against own products
9. THE Auth_System SHALL define the Borrower role as able to apply for loans, view own applications, and make payments
10. THE Auth_System SHALL define the Auditor role as having read-only access to all platform data for compliance purposes

### Requirement 7: Role Assignment and Management

**User Story:** As an administrator, I want to assign and remove roles from users, so that I can control platform access based on organizational responsibilities.

#### Acceptance Criteria

1. WHEN an Admin assigns a role to a user, THE Auth_System SHALL add the role to the user in the Identity_Store and return a success response
2. WHEN an Admin removes a role from a user, THE Auth_System SHALL remove the role from the user in the Identity_Store and return a success response
3. THE Auth_System SHALL allow a single user to hold multiple roles simultaneously
4. WHEN a role assignment or removal occurs, THE Audit_Logger SHALL record the event including the admin user identifier, target user identifier, role name, and action type
5. WHEN a non-Admin user attempts to assign or remove roles, THE Authorization_Engine SHALL reject the request and return a 403 Forbidden response
6. THE Auth_System SHALL prevent removal of the last Admin role from the system to ensure administrative access is never lost

### Requirement 8: Policy-Based API Authorization

**User Story:** As a platform architect, I want API endpoints protected by named policies, so that access control is centralized, maintainable, and auditable.

#### Acceptance Criteria

1. THE Authorization_Engine SHALL enforce the CanManageUsers policy on user management endpoints, permitting access only to users with the Admin role
2. THE Authorization_Engine SHALL enforce the CanProcessApplications policy on loan application workflow endpoints (mark-under-review, approve, reject, fund), permitting access only to users with the Admin or CrmManager role
3. THE Authorization_Engine SHALL enforce the CanManageProducts policy on loan product creation and workflow endpoints (create, submit-for-approval, approve, publish, archive), permitting access only to users with the Admin, CrmManager, or Lender role
4. THE Authorization_Engine SHALL enforce the CanViewReports policy on audit log and dashboard endpoints, permitting access only to users with the Admin or Auditor role
5. THE Authorization_Engine SHALL enforce the CanManageLenders policy on lender creation and management endpoints, permitting access only to users with the Admin role
6. THE Authorization_Engine SHALL enforce the CanManageBorrowers policy on borrower creation and management endpoints, permitting access only to users with the Admin or CrmManager role
7. WHEN an unauthenticated request is received for a protected endpoint, THE Authorization_Engine SHALL return a 401 Unauthorized response
8. WHEN an authenticated request without sufficient permissions is received for a protected endpoint, THE Authorization_Engine SHALL return a 403 Forbidden response
9. THE Authorization_Engine SHALL enforce the CanVetUsers policy on registration approval/rejection endpoints, permitting access only to users with the CrmManager role
10. THE Authorization_Engine SHALL enforce the CanApproveProducts policy on loan product approval endpoints, permitting access only to users with the CrmManager or Admin role
11. THE Authorization_Engine SHALL enforce the CanHandleDisputes policy on dispute management endpoints, permitting access only to users with the CustomerService or Admin role
12. THE Authorization_Engine SHALL enforce the CanManageMessages policy on messaging and communication moderation endpoints, permitting access only to users with the CustomerService or Admin role
13. THE Authorization_Engine SHALL enforce the CanSetLimits policy on credit limit and capital limit management endpoints, permitting access only to users with the CrmManager or Admin role
14. THE Authorization_Engine SHALL enforce the CanApproveDisbursements policy on final loan disbursement approval endpoints, permitting access only to users with the Admin role

### Requirement 9: Resource-Based Authorization (Data Isolation)

**User Story:** As a platform user, I want to access only my own data, so that sensitive financial information is protected from unauthorized viewing.

#### Acceptance Criteria

1. WHILE a user has only the Borrower role, THE Authorization_Engine SHALL restrict loan application queries to return only applications belonging to that Borrower
2. WHILE a user has only the Lender role, THE Authorization_Engine SHALL restrict loan product queries to return only products belonging to that Lender
3. WHILE a user has only the Lender role, THE Authorization_Engine SHALL restrict loan application queries to return only applications submitted against that Lender's products
4. WHILE a user has the Admin, CrmManager, or Auditor role, THE Authorization_Engine SHALL permit access to all loan applications and loan products without ownership restriction
5. WHEN a Borrower attempts to access a loan application that does not belong to them, THE Authorization_Engine SHALL return a 403 Forbidden response
6. WHEN a Lender attempts to access a loan product that does not belong to them, THE Authorization_Engine SHALL return a 403 Forbidden response

### Requirement 10: Blazor WASM Authentication State

**User Story:** As a frontend developer, I want the Blazor application to reflect the user's authentication state from JWT claims, so that the UI can render role-appropriate content.

#### Acceptance Criteria

1. THE Auth_State_Provider SHALL read the Access_Token from browser localStorage and parse its claims to construct the authentication state
2. WHEN no valid Access_Token exists in localStorage, THE Auth_State_Provider SHALL report the user as unauthenticated
3. WHEN the Access_Token in localStorage is expired, THE Auth_State_Provider SHALL attempt a token refresh using the stored Refresh_Token before reporting the user as unauthenticated
4. WHEN a successful login occurs, THE Auth_State_Provider SHALL store the Access_Token and Refresh_Token in localStorage and notify all subscribers of the authentication state change
5. WHEN a logout occurs, THE Auth_State_Provider SHALL remove the Access_Token and Refresh_Token from localStorage and notify all subscribers of the authentication state change
6. THE Auth_State_Provider SHALL expose the user's roles and claims to Blazor components for conditional rendering

### Requirement 11: Blazor Protected Routes

**User Story:** As a platform user, I want to be redirected to the login page when accessing protected routes without authentication, so that unauthorized access to the UI is prevented.

#### Acceptance Criteria

1. WHEN an unauthenticated user navigates to a protected route, THE Auth_State_Provider SHALL redirect the user to the login page
2. WHEN an authenticated user without the required role navigates to a role-restricted route, THE Auth_State_Provider SHALL display an access denied message
3. THE Auth_State_Provider SHALL preserve the originally requested URL and redirect the user to it after successful authentication
4. THE Auth_State_Provider SHALL evaluate route authorization using the AuthorizeRouteView component with policy-based attributes

### Requirement 12: Blazor Role-Based UI Rendering

**User Story:** As a platform user, I want to see only the navigation items and actions relevant to my role, so that the interface is uncluttered and role-appropriate.

#### Acceptance Criteria

1. WHILE a user has the Admin role, THE Auth_State_Provider SHALL enable rendering of all navigation items including User Management, Lenders, Borrowers, Loan Products, Loan Applications, Audit Logs, Dashboard, Disputes, and Messages
2. WHILE a user has the CrmManager role, THE Auth_State_Provider SHALL enable rendering of Borrowers, Lenders, Loan Applications, Loan Products (approval), Credit Tiers, Account Limits, and Vetting Queue navigation items
3. WHILE a user has the CustomerService role, THE Auth_State_Provider SHALL enable rendering of Disputes, Messages, FAQ Management, and Late Payment Cases navigation items
4. WHILE a user has the Lender role, THE Auth_State_Provider SHALL enable rendering of Loan Products (own only) and Loan Applications (against own products) navigation items
5. WHILE a user has the Borrower role, THE Auth_State_Provider SHALL enable rendering of Loan Applications (own only) and Payments navigation items
6. WHILE a user has the Auditor role, THE Auth_State_Provider SHALL enable rendering of all navigation items in read-only mode without action buttons
7. THE Auth_State_Provider SHALL hide workflow action buttons (approve, reject, fund, publish, archive, vet, set-limits) from users whose roles do not include the corresponding policy

### Requirement 13: Token Auto-Refresh in Blazor

**User Story:** As an authenticated user, I want my tokens refreshed automatically before expiry, so that my session continues without interruption.

#### Acceptance Criteria

1. THE Auth_State_Provider SHALL monitor the Access_Token expiration and initiate a refresh request 2 minutes before the token expires
2. WHEN the automatic token refresh succeeds, THE Auth_State_Provider SHALL update the stored tokens in localStorage without disrupting the user's current activity
3. WHEN the automatic token refresh fails, THE Auth_State_Provider SHALL redirect the user to the login page with a session expired notification
4. THE Auth_State_Provider SHALL attach the current valid Access_Token as a Bearer token in the Authorization header of all outgoing API requests

### Requirement 14: User Management Administration

**User Story:** As an administrator, I want to manage all user accounts from a dedicated interface, so that I can maintain platform access control efficiently.

#### Acceptance Criteria

1. WHEN an Admin requests the user list, THE User_Management_Module SHALL return all users with their assigned roles, account status, and last login timestamp
2. WHEN an Admin creates a new user, THE User_Management_Module SHALL create the ApplicationUser with the specified email, assign the specified roles, and return the new user identifier
3. WHEN an Admin updates a user's details, THE User_Management_Module SHALL persist the changes to the Identity_Store and return a success response
4. WHEN an Admin disables a user account, THE User_Management_Module SHALL set the account status to disabled, invalidate all active Refresh_Tokens for that user, and return a success response
5. WHEN an Admin enables a previously disabled user account, THE User_Management_Module SHALL set the account status to enabled and return a success response
6. WHEN a non-Admin user attempts to access user management endpoints, THE Authorization_Engine SHALL return a 403 Forbidden response

### Requirement 15: Authentication Audit Logging

**User Story:** As a compliance officer, I want all authentication events logged, so that security incidents can be investigated and regulatory requirements are met.

#### Acceptance Criteria

1. WHEN a successful login occurs, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "Login", and a description including the user email and IP address
2. WHEN a failed login attempt occurs, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "LoginFailed", and a description including the attempted email and IP address
3. WHEN an account lockout occurs, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "AccountLocked", and a description including the user email and lockout duration
4. WHEN a password reset is completed, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "PasswordReset", and a description including the user email
5. WHEN a role assignment or removal occurs, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "RoleChanged", and a description including the target user email, role name, and whether it was added or removed
6. WHEN a user account is disabled or enabled, THE Audit_Logger SHALL create an AuditLog entry with EntityName "ApplicationUser", Action "AccountStatusChanged", and a description including the target user email and new status

### Requirement 16: Security Transport and CORS

**User Story:** As a security architect, I want all communication secured with HTTPS and CORS properly configured, so that the platform is protected against transport-layer and cross-origin attacks.

#### Acceptance Criteria

1. THE Auth_System SHALL require HTTPS for all API endpoints in production environments
2. THE Auth_System SHALL configure CORS to allow requests only from the Blazor WebAssembly application origin
3. THE Auth_System SHALL include the Authorization header in the CORS allowed headers configuration
4. THE Auth_System SHALL reject requests from origins not in the configured allowed origins list by omitting CORS headers from the response
5. IF the application is running in a non-production environment, THEN THE Auth_System SHALL allow configurable CORS origins from application settings for development flexibility

### Requirement 17: JWT Configuration and Validation

**User Story:** As a platform architect, I want JWT tokens validated with strong cryptographic settings, so that token forgery and tampering are prevented.

#### Acceptance Criteria

1. THE Token_Service SHALL sign Access_Tokens using HMAC-SHA256 with a secret key of at least 256 bits configured in application settings
2. THE Token_Service SHALL include the issuer and audience claims in the Access_Token and validate both during token verification
3. THE Auth_System SHALL reject tokens with invalid signatures, expired timestamps, or mismatched issuer/audience claims
4. THE Auth_System SHALL read JWT configuration (secret key, issuer, audience, token lifetimes) from application settings with no hardcoded values
5. IF the configured JWT secret key is shorter than 256 bits, THEN THE Auth_System SHALL throw a configuration exception at application startup with a descriptive error message


### Requirement 18: Account Status Management

**User Story:** As an administrator, I want to manage user account statuses with granular control, so that I can restrict or revoke platform access based on compliance and operational needs.

#### Acceptance Criteria

1. THE User_Management_Module SHALL support the following Account_Status values: Active, Hold, Blocked, Suspended, and Closed
2. WHILE an account has the Hold status, THE Auth_System SHALL prevent the user from creating new loan applications or new loan products while allowing existing loans and lending activities to continue
3. WHILE an account has the Blocked status, THE Auth_System SHALL prevent the user from performing the specific blocked activity (borrowing OR lending) as configured by the Admin
4. WHILE an account has the Suspended status, THE Auth_System SHALL revoke all platform access and reject all API requests from that user with a 403 Forbidden response indicating account suspension
5. WHILE an account has the Closed status, THE Auth_System SHALL permanently prevent all login attempts and API access, and return an error indicating the account is permanently closed
6. WHEN an Admin changes an account status, THE User_Management_Module SHALL require a mandatory reason text explaining the status change
7. WHEN an account status change occurs, THE Audit_Logger SHALL record the event including the admin user identifier, target user identifier, previous status, new status, reason, and timestamp
8. WHEN an account status is changed to Hold, Blocked, Suspended, or Closed, THE Auth_System SHALL send an email notification to the affected user explaining the status change and reason
9. WHEN an account status is changed to Hold, Blocked, Suspended, or Closed, THE Auth_System SHALL send an in-app notification to the affected user visible upon next login attempt
10. WHEN an Admin sets an account to Blocked status, THE User_Management_Module SHALL require specification of which activity is blocked (borrowing, lending, or both)

### Requirement 19: Credit Scoring and Tier System

**User Story:** As a CRM Manager, I want to assign credit tiers to borrowers based on their financial profile, so that lenders can assess risk and appropriate interest rates are applied.

#### Acceptance Criteria

1. THE Auth_System SHALL define three Credit_Tier levels: Tier A (excellent), Tier B (good), and Tier C (fair)
2. THE Auth_System SHALL associate Tier A with an interest rate range of 10-11% and high loan limits
3. THE Auth_System SHALL associate Tier B with an interest rate range of 12-13% and medium loan limits
4. THE Auth_System SHALL associate Tier C with an interest rate range of 14-15% and lower loan limits
5. WHEN a CrmManager assigns a Credit_Tier to a Borrower, THE Auth_System SHALL update the Borrower profile with the assigned tier and record the assignment in the audit log
6. WHEN a CrmManager overrides a calculated Credit_Tier, THE Auth_System SHALL require a written justification and record the override reason in the audit log
7. THE Auth_System SHALL display the assigned Credit_Tier on the Borrower profile visible to users with the Lender, CrmManager, Admin, or Auditor role
8. WHEN a Borrower views their own profile, THE Auth_System SHALL display their assigned Credit_Tier and associated interest rate range
9. WHEN a CrmManager assigns or changes a Credit_Tier, THE Audit_Logger SHALL record the event including the CrmManager identifier, Borrower identifier, previous tier, new tier, and justification

### Requirement 20: Account Limits Management

**User Story:** As a CRM Manager, I want to set borrowing and lending limits for users, so that financial exposure is controlled according to each user's risk profile and platform policies.

#### Acceptance Criteria

1. WHEN a CrmManager sets a credit limit for a Borrower, THE Auth_System SHALL store the limit and enforce it on all subsequent loan applications from that Borrower
2. WHEN a CrmManager sets a capital limit for a Lender, THE Auth_System SHALL store the limit and enforce it on all subsequent loan product funding from that Lender
3. WHEN an Admin overrides a credit limit or capital limit set by a CrmManager, THE Auth_System SHALL update the limit and record the override in the audit log with the Admin identifier and reason
4. THE Auth_System SHALL enforce a maximum number of active loans per Borrower, configurable by the Admin through system settings
5. WHEN a Borrower attempts to apply for a loan that would exceed their credit limit, THE Auth_System SHALL reject the application and return an error indicating the credit limit would be exceeded
6. WHEN a Lender attempts to fund a loan that would exceed their capital limit, THE Auth_System SHALL reject the funding and return an error indicating the capital limit would be exceeded
7. WHEN a Borrower attempts to apply for a loan that would exceed the maximum active loans count, THE Auth_System SHALL reject the application and return an error indicating the maximum active loans limit has been reached
8. THE Auth_System SHALL enforce transaction limits per role as configured by the Admin through system settings
9. WHEN a limit is set or changed, THE Audit_Logger SHALL record the event including the actor identifier, target user identifier, limit type, previous value, new value, and timestamp

### Requirement 21: Two-Factor Authentication

**User Story:** As a platform user, I want to enable two-factor authentication on my account, so that my account is protected with an additional security layer beyond my password.

#### Acceptance Criteria

1. THE Auth_System SHALL support TOTP-based two-factor authentication compatible with standard authenticator applications (Google Authenticator, Microsoft Authenticator, Authy)
2. WHEN a user enables two-factor authentication, THE Auth_System SHALL generate a TOTP secret key, display it as a QR code, and require verification of a valid TOTP code before activation
3. WHEN two-factor authentication is enabled for an account, THE Auth_System SHALL require a valid TOTP code in addition to email and password during login
4. THE Auth_System SHALL enforce mandatory two-factor authentication for all users with the Admin or CrmManager role
5. WHEN a user with a mandatory 2FA role attempts to log in without 2FA configured, THE Auth_System SHALL redirect the user to the 2FA setup flow and prevent platform access until 2FA is configured
6. WHEN a user enables two-factor authentication, THE Auth_System SHALL generate 10 single-use backup codes for account recovery
7. WHEN a user submits a valid backup code during login, THE Auth_System SHALL authenticate the user and mark the backup code as used
8. WHEN an Admin enforces two-factor authentication for a specific role, THE Auth_System SHALL require all users with that role to configure 2FA on their next login
9. WHEN a user's backup codes are exhausted, THE Auth_System SHALL allow the user to generate a new set of backup codes after re-authenticating with their password and a valid TOTP code
10. WHEN two-factor authentication is enabled or disabled for an account, THE Audit_Logger SHALL record the event including the user identifier and timestamp

### Requirement 22: User Vetting Workflow

**User Story:** As a CRM Manager, I want to review and approve new user registrations before granting platform access, so that only verified and compliant users can participate in the lending marketplace.

#### Acceptance Criteria

1. WHEN a new user completes registration and email verification, THE Auth_System SHALL place the account in "Pending Approval" status and add it to the CrmManager vetting queue
2. WHILE an account is in "Pending Approval" status, THE Auth_System SHALL prevent the user from accessing any platform features except viewing their approval status
3. WHEN a CrmManager approves a registration, THE Auth_System SHALL change the Account_Status to Active and notify the user via email that their account is approved
4. WHEN a CrmManager rejects a registration, THE Auth_System SHALL change the Account_Status to Closed, record the rejection reason, and notify the user via email with the rejection reason
5. WHEN a CrmManager reviews a registration, THE User_Management_Module SHALL allow the CrmManager to request additional documents from the applicant
6. WHEN additional documents are requested, THE Auth_System SHALL notify the user via email and in-app notification specifying which documents are required
7. WHEN a vetting decision (approve, reject, or request documents) is made, THE Audit_Logger SHALL record the event including the CrmManager identifier, applicant identifier, decision, reason, and timestamp
8. WHEN a vetting decision is made, THE Auth_System SHALL notify the Admin of the decision via in-app notification
9. THE User_Management_Module SHALL provide a vetting queue interface showing all pending registrations with submission date, user type (Borrower/Lender), and documents provided

### Requirement 23: Granular Permission System

**User Story:** As an administrator, I want to define fine-grained permissions grouped by module, so that I can create custom roles with precise access control tailored to organizational needs.

#### Acceptance Criteria

1. THE Authorization_Engine SHALL organize permissions into the following modules: Users, Loans, Products, Finance, Reports, Settings, and Messages
2. THE Authorization_Engine SHALL support the following actions for each permission module: View, Create, Edit, Delete, and Approve
3. WHEN an Admin creates a custom role, THE Auth_System SHALL allow selection of specific permission-action combinations from any module
4. WHEN a custom role is assigned to a user, THE Authorization_Engine SHALL evaluate the granular permissions in addition to the predefined role policies
5. THE User_Management_Module SHALL provide a permission testing tool that simulates a specified user's access to any endpoint or resource without executing the action
6. WHEN an Admin modifies a custom role's permissions, THE Auth_System SHALL apply the changes to all users holding that role on their next token refresh
7. WHEN a custom role is created, modified, or deleted, THE Audit_Logger SHALL record the event including the Admin identifier, role name, permissions changed, and timestamp
8. THE Authorization_Engine SHALL resolve permission conflicts by applying the most permissive rule when a user holds multiple roles with overlapping permissions

### Requirement 24: Login Remember Me and Session Extension

**User Story:** As a returning user, I want to stay logged in across browser sessions when I choose to, so that I do not need to re-authenticate on every visit.

#### Acceptance Criteria

1. WHEN a user selects the "Remember Me" option during login, THE Token_Service SHALL issue a Refresh_Token with a 30-day expiration instead of the standard 7-day expiration
2. WHEN a user logs in without selecting "Remember Me", THE Token_Service SHALL issue a Refresh_Token with the standard 7-day expiration
3. THE Auth_State_Provider SHALL persist the "Remember Me" preference in browser localStorage alongside the tokens
4. WHEN a remembered session's Refresh_Token is used for token refresh, THE Token_Service SHALL issue a new Refresh_Token maintaining the 30-day expiration window
5. WHEN a user explicitly logs out, THE Auth_System SHALL invalidate all tokens regardless of the "Remember Me" setting


### Requirement 18: Enhanced Role System with CRM Manager and Customer Service

**User Story:** As a platform architect, I want a comprehensive role system matching financial services operations, so that each team member has appropriate access for their responsibilities.

#### Acceptance Criteria

1. THE Auth_System SHALL define the following roles in the Identity_Store: Admin, CrmManager, CustomerService, Lender, and Borrower
2. THE Auth_System SHALL define the CrmManager role as authorized to vet and approve user registrations, set borrower credit limits, assign credit tiers, approve loan products, and perform AML compliance checks
3. THE Auth_System SHALL define the CustomerService role as authorized to handle support tickets, manage disputes, moderate messaging, manage FAQ content, and handle late payment cases
4. THE Auth_System SHALL define the Admin role as having full override capability on all CrmManager and CustomerService decisions
5. WHEN a user with the CrmManager role logs in, THE Auth_State_Provider SHALL render a CRM-specific dashboard displaying pending approvals, vetting queue, credit limit requests, and compliance alerts
6. WHEN a user with the CustomerService role logs in, THE Auth_State_Provider SHALL render a support-specific dashboard displaying open tickets, active disputes, pending messages, and late payment cases
7. WHEN a user with the Lender role logs in, THE Auth_State_Provider SHALL render a lender dashboard displaying own loan products, applications against own products, and portfolio summary
8. WHEN a user with the Borrower role logs in, THE Auth_State_Provider SHALL render a borrower dashboard displaying own loan applications, payment schedule, and available loan products

### Requirement 19: Account Status Management

**User Story:** As an administrator, I want granular account status controls, so that I can manage risk and compliance with appropriate severity levels.

#### Acceptance Criteria

1. THE Auth_System SHALL support the following Account_Status values: Active, Hold, Blocked, Suspended, and Closed
2. WHILE an account has the Hold status, THE Auth_System SHALL prevent the user from creating new loan applications or loan products while allowing continued access to existing loans and platform login
3. WHILE an account has the Blocked status, THE Auth_System SHALL prevent the user from performing the specific blocked activity (borrowing or lending) while allowing platform login and access to non-blocked features
4. WHILE an account has the Suspended status, THE Auth_System SHALL revoke all platform access, invalidate all active sessions and Refresh_Tokens, and reject all login attempts for that account
5. WHILE an account has the Closed status, THE Auth_System SHALL permanently prevent login and reject all authentication attempts for that account
6. WHEN an Admin or CrmManager changes an account status, THE Auth_System SHALL require a mandatory reason field in the status change request
7. WHEN an account status change occurs, THE Audit_Logger SHALL record the event including the admin identifier, target user identifier, previous status, new status, reason, and timestamp
8. WHEN an account status is changed to Hold, Blocked, Suspended, or Closed, THE Auth_System SHALL send an email notification and an in-app notification to the affected user explaining the status change and reason
9. WHEN an Admin requests reversal of a Hold or Blocked status, THE Auth_System SHALL require a justification field and restore the account to Active status upon submission
10. THE Auth_System SHALL prevent any status change that would remove the last Active Admin account from the platform

### Requirement 20: CRM Vetting and User Approval Workflow

**User Story:** As a CRM Manager, I want to vet new user registrations before granting platform access, so that only verified users can participate in financial transactions.

#### Acceptance Criteria

1. WHEN a new Borrower or Lender completes registration and email verification, THE Auth_System SHALL set the account status to PendingApproval
2. WHILE an account has the PendingApproval status, THE Auth_System SHALL prevent the user from accessing platform features beyond viewing their own profile
3. WHEN a CrmManager views their dashboard, THE User_Management_Module SHALL display all accounts with PendingApproval status in a vetting queue sorted by registration date
4. WHEN a CrmManager approves a registration, THE Vetting_Workflow SHALL require a mandatory approval reason and set the account status to Active
5. WHEN a CrmManager rejects a registration, THE Vetting_Workflow SHALL require a mandatory rejection reason and set the account status to Closed
6. WHEN a CrmManager requests additional documents from an applicant, THE Vetting_Workflow SHALL send a notification to the applicant specifying the required documents and set the account status to DocumentsRequested
7. WHEN a CrmManager approves a Borrower, THE Vetting_Workflow SHALL require the CrmManager to assign a Credit_Tier (A, B, or C) and a borrower credit limit
8. WHEN a CrmManager approves a Lender, THE Vetting_Workflow SHALL require the CrmManager to assign a capital limit
9. WHEN a vetting decision is made, THE Audit_Logger SHALL record the event including the CrmManager identifier, target user identifier, decision type, reason, and any assigned limits or tiers
10. WHEN an Admin overrides a CrmManager vetting decision, THE Auth_System SHALL require a justification field and THE Audit_Logger SHALL record the override event with the Admin identifier and justification

### Requirement 21: Credit Scoring and Limits

**User Story:** As a CRM Manager, I want to assign credit tiers and limits to users, so that lending risk is managed appropriately.

#### Acceptance Criteria

1. THE Auth_System SHALL define Credit_Tier values: A (excellent), B (good), and C (fair)
2. WHEN a CrmManager assigns a Credit_Tier to a Borrower, THE Auth_System SHALL associate the tier with predefined interest rate ranges and maximum loan amount limits
3. THE Auth_System SHALL enforce that a Borrower credit limit restricts the maximum total outstanding loan amount for that Borrower
4. THE Auth_System SHALL enforce that a Lender capital limit restricts the maximum total outstanding lending amount for that Lender
5. WHEN a Borrower submits a loan application exceeding their credit limit, THE Auth_System SHALL reject the application and return an error indicating the credit limit would be exceeded
6. WHEN a Lender creates a loan product with a total value exceeding their capital limit, THE Auth_System SHALL reject the product creation and return an error indicating the capital limit would be exceeded
7. WHEN an Admin overrides a credit limit or capital limit, THE Auth_System SHALL require a justification field and THE Audit_Logger SHALL record the override event with the Admin identifier, previous limit, new limit, and justification
8. WHILE a user views a Borrower or Lender profile, THE Auth_State_Provider SHALL display the assigned credit tier and limits only to users with the Admin, CrmManager, or Auditor role

### Requirement 22: Two-Factor Authentication

**User Story:** As a security-conscious user, I want optional two-factor authentication, so that my account has an additional layer of protection.

#### Acceptance Criteria

1. THE Auth_System SHALL support TOTP-based Two_Factor_Auth compatible with Google Authenticator and Microsoft Authenticator applications
2. WHEN a user enables Two_Factor_Auth from account settings, THE Auth_System SHALL generate a shared secret, display a QR code for authenticator app enrollment, and require a valid TOTP code to confirm activation
3. WHEN a user successfully enables Two_Factor_Auth, THE Auth_System SHALL generate and display a set of 10 single-use Recovery_Codes for backup access
4. WHEN a user with Two_Factor_Auth enabled submits valid credentials during login, THE Auth_System SHALL prompt for a TOTP code before issuing tokens
5. WHEN an Admin configures Two_Factor_Auth as mandatory for a specific role, THE Auth_System SHALL require users with that role to enable Two_Factor_Auth before accessing protected resources
6. WHEN an Admin designates an action as high-risk, THE Auth_System SHALL require Two_Factor_Auth verification before executing that action for users with Two_Factor_Auth enabled
7. WHEN a user enables or disables Two_Factor_Auth, THE Audit_Logger SHALL record the event including the user identifier, action type, and timestamp
8. WHEN a user submits a valid Recovery_Code during login, THE Auth_System SHALL accept the code in place of a TOTP code and invalidate that specific Recovery_Code

### Requirement 23: Custom Role Creation and Granular Permissions

**User Story:** As an administrator, I want to create custom roles with granular permissions, so that I can adapt access control to evolving organizational needs.

#### Acceptance Criteria

1. WHEN an Admin creates a custom role, THE Auth_System SHALL persist the Custom_Role in the Identity_Store with a unique name and description
2. THE Auth_System SHALL define Permission_Module categories: UserManagement, LoanManagement, FinancialOperations, Reports, SystemSettings, and Messaging
3. THE Auth_System SHALL define granular actions within each Permission_Module: View, Create, Edit, Delete, and Approve
4. WHEN an Admin assigns permissions to a Custom_Role, THE Auth_System SHALL associate the selected Permission_Module and action combinations with that role
5. THE Auth_System SHALL allow a single user to hold multiple roles simultaneously, including combinations of predefined and custom roles
6. WHEN a user holds multiple roles, THE Authorization_Engine SHALL evaluate the combined permissions from all assigned roles and grant access if any assigned role permits the requested action
7. WHEN an Admin requests a permission simulation for a user, THE Auth_System SHALL return the complete list of accessible resources and actions for that user based on all assigned roles
8. WHEN a role or permission assignment changes, THE Audit_Logger SHALL record the event including the Admin identifier, target role or user, and the specific permissions added or removed
9. THE Auth_System SHALL prevent deletion of predefined roles (Admin, CrmManager, CustomerService, Lender, Borrower, Auditor) and return an error if deletion is attempted

### Requirement 24: Session Management and Remember Me

**User Story:** As a returning user, I want persistent login sessions and visibility into my active sessions, so that I can balance convenience with security.

#### Acceptance Criteria

1. WHEN a user selects the "Remember Me" option during login, THE Token_Service SHALL set the Refresh_Token expiration to 30 days instead of the default 7 days
2. THE Session_Manager SHALL track all active sessions per user including device type, IP address, and last activity timestamp
3. WHEN a user views their account settings, THE Session_Manager SHALL display all active sessions for that user with device and location information
4. WHEN a user revokes a specific session from account settings, THE Session_Manager SHALL invalidate the Refresh_Token for that session and terminate it immediately
5. WHEN an Admin views a user's profile, THE Session_Manager SHALL display all active sessions for that user and allow the Admin to terminate any session
6. WHEN a user changes their password, THE Session_Manager SHALL invalidate all active Refresh_Tokens and terminate all sessions for that user except the current session
7. WHEN an account status is changed to Suspended, THE Session_Manager SHALL invalidate all active Refresh_Tokens and terminate all sessions for that user immediately


### Requirement 18: Enhanced Role System with CRM Manager and Customer Service

**User Story:** As a platform architect, I want a comprehensive role system matching financial services operations, so that each team member has appropriate access for their responsibilities.

#### Acceptance Criteria

1. THE Auth_System SHALL define the following roles: Admin, CRMManager, CustomerService, Lender, Borrower
2. WHEN a user has the CRMManager role, THE Authorization_Engine SHALL permit access to vet and approve borrower/lender registrations, set credit limits, and approve loan products before publication
3. WHEN a user has the CustomerService role, THE Authorization_Engine SHALL permit access to handle support tickets, manage disputes, mediate communications between borrowers and lenders, and manage the FAQ knowledge base
4. WHEN a user has the Admin role, THE Authorization_Engine SHALL permit full override capability on all CRM Manager decisions with mandatory justification
5. THE Financial_UI_System SHALL provide each role with a dedicated dashboard displaying role-appropriate content, metrics, and action items
6. THE Auth_System SHALL support the CRMManager role having access to a vetting queue with pending registrations, loan product approvals, and risk assessment tools
7. THE Auth_System SHALL support the CustomerService role having access to open tickets, dispute management, messaging center, and FAQ administration

### Requirement 19: Account Status Management (Hold/Block/Suspend)

**User Story:** As an administrator, I want granular account status controls, so that I can manage risk and compliance with appropriate severity levels.

#### Acceptance Criteria

1. THE Auth_System SHALL provide the following account statuses: Active, Hold, Blocked, Suspended, Closed
2. WHEN an account is placed on Hold, THE Auth_System SHALL prevent new loan applications or lending activity while allowing existing loans to continue normally
3. WHEN an account is Blocked, THE Auth_System SHALL prevent specific activities (borrowing OR lending) while still allowing the user to log in and make payments on existing loans
4. WHEN an account is Suspended, THE Auth_System SHALL revoke all platform access immediately, log the user out of all active sessions, and freeze all activities
5. WHEN an account is Closed, THE Auth_System SHALL permanently disable the account with no possibility of reactivation
6. WHEN any account status change occurs, THE Auth_System SHALL require a mandatory reason from the administrator performing the action
7. WHEN any account status change occurs, THE Audit_Logger SHALL record the change with admin identifier, timestamp, previous status, new status, and reason
8. WHEN an account status changes, THE Notification_System SHALL notify the affected user via email and in-app notification with the reason for the change
9. THE Auth_System SHALL allow administrators to reverse Hold and Blocked statuses with mandatory justification
10. THE Auth_System SHALL prevent the last Admin account from being Suspended or Closed

### Requirement 20: CRM Vetting and User Approval Workflow

**User Story:** As a CRM Manager, I want to vet new user registrations before granting platform access, so that only verified users can participate in financial transactions.

#### Acceptance Criteria

1. WHEN a new Borrower or Lender registers, THE Auth_System SHALL create the account in "PendingApproval" status with no access to platform features
2. THE User_Management_Module SHALL display pending registrations in the CRM Manager dashboard as a prioritized vetting queue
3. WHEN a CRM Manager approves a registration, THE Auth_System SHALL update the account status to Active and grant role-appropriate access
4. WHEN a CRM Manager rejects a registration, THE Auth_System SHALL update the account status to Rejected and send a notification with the rejection reason
5. THE User_Management_Module SHALL allow CRM Managers to request additional documents from applicants during the vetting process
6. WHEN a CRM Manager approves a Borrower, THE Auth_System SHALL require the CRM Manager to set a credit limit and assign a credit tier (A, B, or C)
7. WHEN a CRM Manager approves a Lender, THE Auth_System SHALL require the CRM Manager to set a capital limit based on verified financial capacity
8. WHEN an Admin overrides a CRM Manager decision, THE Audit_Logger SHALL record the override with the Admin identifier, original decision, new decision, and justification
9. THE Auth_System SHALL send reminder notifications to CRM Managers for registrations pending longer than 48 hours

### Requirement 21: Credit Scoring and Limits

**User Story:** As a CRM Manager, I want to assign credit tiers and limits to users, so that lending risk is managed appropriately.

#### Acceptance Criteria

1. THE Auth_System SHALL define three credit tiers: Tier A (excellent credit, lower interest rates, higher limits), Tier B (good credit, moderate rates and limits), Tier C (fair credit, higher rates, lower limits)
2. WHEN a CRM Manager assigns a credit tier, THE Auth_System SHALL store the tier on the Borrower profile and make it visible to authorized roles (Admin, CRMManager, Lender)
3. THE Authorization_Engine SHALL enforce borrower credit limits at loan application time, rejecting applications that exceed the assigned limit
4. THE Authorization_Engine SHALL enforce lender capital limits at loan product creation time, preventing products that exceed the assigned limit
5. WHEN an Admin overrides a credit limit or tier, THE Audit_Logger SHALL record the override with previous value, new value, and justification
6. THE Dashboard_Analytics SHALL display credit tier distribution and limit utilization metrics for Admin and CRMManager roles
7. THE Auth_System SHALL allow CRM Managers to re-assess and update credit tiers during periodic reviews with audit trail

### Requirement 22: Two-Factor Authentication

**User Story:** As a security-conscious user, I want optional two-factor authentication, so that my account has an additional layer of protection.

#### Acceptance Criteria

1. THE Auth_System SHALL support TOTP-based two-factor authentication compatible with Google Authenticator and Microsoft Authenticator
2. WHEN a user enables 2FA, THE Auth_System SHALL generate and display a QR code for authenticator app setup and provide backup recovery codes
3. WHEN 2FA is enabled for a user, THE Auth_System SHALL require a valid TOTP code after successful password verification during login
4. THE Auth_System SHALL allow administrators to enforce mandatory 2FA for specific roles (Admin and CRMManager by default)
5. WHEN a user presents a valid backup recovery code, THE Auth_System SHALL allow login and mark the recovery code as used
6. THE Auth_System SHALL allow administrators to configure high-risk actions that require 2FA verification regardless of session state (e.g., changing account limits, approving large loans)
7. WHEN 2FA is enabled or disabled, THE Audit_Logger SHALL record the event with user identifier and timestamp

### Requirement 23: Custom Role Creation and Granular Permissions

**User Story:** As an administrator, I want to create custom roles with granular permissions, so that I can adapt access control to evolving organizational needs.

#### Acceptance Criteria

1. THE Auth_System SHALL allow administrators to create custom roles beyond the predefined set (Admin, CRMManager, CustomerService, Lender, Borrower)
2. THE Auth_System SHALL provide granular permission categories: UserManagement, LoanManagement, FinancialOperations, Reports, SystemSettings, Messaging, ProductManagement
3. WITHIN each permission category, THE Auth_System SHALL support individual permission levels: View, Create, Edit, Delete, Approve
4. WHEN a user holds multiple roles, THE Authorization_Engine SHALL combine permissions from all assigned roles using a union (most permissive) strategy
5. THE User_Management_Module SHALL provide a permission testing tool that allows administrators to simulate what a specific user can access based on their assigned roles
6. WHEN a custom role is created, modified, or deleted, THE Audit_Logger SHALL record the change with administrator identifier, role details, and timestamp
7. THE Auth_System SHALL prevent deletion or modification of predefined system roles (Admin, CRMManager, CustomerService, Lender, Borrower)

### Requirement 24: Remember Me and Session Management

**User Story:** As a returning user, I want persistent login sessions and visibility into my active sessions, so that I can balance convenience with security.

#### Acceptance Criteria

1. WHEN a user selects "Remember Me" during login, THE Token_Service SHALL extend the Refresh_Token lifetime to 30 days (versus the default 7 days)
2. THE Auth_System SHALL track active sessions per user including device type, IP address, browser, and last activity timestamp
3. THE Auth_State_Provider SHALL provide a "My Sessions" view where users can see all their active sessions and revoke any session
4. WHEN a user changes their password, THE Auth_System SHALL terminate all other active sessions for that user
5. WHEN an account is Suspended, THE Auth_System SHALL immediately terminate all active sessions for that user
6. THE User_Management_Module SHALL allow administrators to view and terminate any user's active sessions
7. THE Auth_System SHALL automatically terminate sessions that have been inactive for longer than the configured timeout period (default: 30 minutes for non-Remember-Me sessions)
