# Implementation Plan: User Roles & Authorization

## Overview

This plan implements a comprehensive authentication and authorization system for the Loan Investment Supermarket platform using ASP.NET Core Identity, JWT-based authentication, policy-based authorization, resource-based data isolation, and Blazor WebAssembly integration. The implementation follows the migration strategy outlined in the design: Schema → Seeding → Link Entities → Protect Endpoints → Blazor Integration.

## Tasks

- [x] 1. Set up Identity infrastructure and domain entities
  - [x] 1.1 Create domain entities and enums for Identity
    - Create `Domain/Entities/Identity/ApplicationUser.cs` extending `IdentityUser` with platform-specific properties (AccountStatus, CreditTier, CreditLimit, CapitalLimit, BlockedActivity, TwoFactorSetupComplete, etc.)
    - Create `Domain/Entities/Identity/RefreshToken.cs` with Token, UserId, ExpiresAtUtc, RevokedAtUtc, ReplacedByToken, IsRememberMe, and computed properties (IsExpired, IsRevoked, IsActive)
    - Create `Domain/Entities/Identity/UserSession.cs` with UserId, RefreshTokenId, DeviceType, IpAddress, Browser, LastActivityAtUtc, IsActive
    - Create `Domain/Entities/Identity/CustomRole.cs` extending `IdentityRole` with Description, IsSystemRole, CreatedAtUtc, CreatedBy
    - Create `Domain/Entities/Identity/RolePermission.cs` with RoleId, Module, Action, GrantedAtUtc, GrantedBy
    - Create `Domain/Entities/Identity/RecoveryCode.cs` with UserId, Code, IsUsed, UsedAtUtc
    - Create `Domain/Enums/AccountStatus.cs` (PendingApproval, Active, Hold, Blocked, Suspended, Closed, DocumentsRequested)
    - Create `Domain/Enums/CreditTier.cs` (A, B, C)
    - Create `Domain/Enums/PermissionModule.cs` and `Domain/Enums/PermissionAction.cs`
    - _Requirements: 1.1, 1.5, 1.6, 6.1, 18.1, 19.1, 21.1, 23.1, 23.2, 23.3_

  - [x] 1.2 Create AuthIdentityDbContext and EF Core configuration
    - Create `Infrastructure/Identity/AuthIdentityDbContext.cs` inheriting from `IdentityDbContext<ApplicationUser, CustomRole, string>`
    - Add DbSets for RefreshToken, UserSession, RolePermission, RecoveryCode
    - Configure entity relationships and indexes (unique index on RefreshToken.Token, index on UserId columns)
    - Add nullable `UserId` FK property to existing `Borrower` and `Lender` entities
    - Register `AuthIdentityDbContext` in DI alongside existing `ApplicationDbContext`
    - _Requirements: 4.4, 9.1, 9.2, 9.3_

  - [x] 1.3 Create EF Core migration for Identity tables
    - Generate initial migration for AuthIdentityDbContext creating AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens, RefreshTokens, UserSessions, RolePermissions, RecoveryCodes tables
    - Generate migration adding nullable UserId FK to Borrower and Lender tables
    - _Requirements: 1.1, 4.4_

  - [x] 1.4 Create application layer interfaces
    - Create `Application/Common/Interfaces/ITokenService.cs` with GenerateTokensAsync, RefreshTokenAsync, RevokeTokenAsync, RevokeAllUserTokensAsync
    - Create `Application/Common/Interfaces/ICurrentUserService.cs` with UserId, Email, Roles, IsAuthenticated, IsInRole, HasPermission
    - Create `Application/Common/Interfaces/IIdentityService.cs` with RegisterUserAsync, ValidateCredentialsAsync, GetUserByEmailAsync, GetUserByIdAsync, GetUserRolesAsync, AssignRoleAsync, RemoveRoleAsync, etc.
    - Create `Application/Common/Interfaces/ITwoFactorService.cs` with GenerateSetupAsync, VerifyCodeAsync, GenerateRecoveryCodesAsync, ValidateRecoveryCodeAsync, EnableAsync, DisableAsync
    - Create `Application/Common/Interfaces/ISessionService.cs` with CreateSessionAsync, GetUserSessionsAsync, RevokeSessionAsync, RevokeAllSessionsAsync, UpdateActivityAsync
    - _Requirements: 2.1, 4.1, 9.1, 21.1, 24.2_

  - [x] 1.5 Create DTOs and response models
    - Create `Application/Features/Auth/Models/AuthTokenResponse.cs` (AccessToken, RefreshToken, ExpiresAt)
    - Create `Application/Features/Auth/Models/LoginRequest.cs`, `RegisterRequest.cs`, `ResetPasswordRequest.cs`
    - Create `Application/Features/Auth/Models/TwoFactorSetupResponse.cs` (SharedKey, QrCodeUri)
    - Create `Application/Features/Users/Models/UserDto.cs`, `UserDetailDto.cs`, `VettingItemDto.cs`, `UserSessionDto.cs`
    - Create `Application/Features/Roles/Models/RoleDto.cs`, `PermissionDto.cs`, `PermissionSimulationResult.cs`
    - Create `Application/Features/Users/Models/CurrentUserDto.cs`
    - Create `Shared/Configuration/JwtSettings.cs` and `AccountSettings.cs` configuration models
    - _Requirements: 2.1, 2.2, 14.1, 22.9, 23.5_

- [x] 2. Checkpoint - Ensure project compiles with new entities and interfaces
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Implement JWT token service and authentication
  - [x] 3.1 Implement JwtTokenService
    - Create `Infrastructure/Identity/JwtTokenService.cs` implementing `ITokenService`
    - Implement JWT generation with user claims (sub, email, given_name, family_name, roles, permissions, account_status)
    - Implement refresh token generation with secure random token, rotation logic, and reuse detection
    - Implement configurable expiration (15 min access, 7 day refresh, 30 day remember-me)
    - Validate JWT secret key length at startup (minimum 256 bits)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1, 4.3, 4.5, 17.1, 17.2, 17.4, 17.5, 24.1, 24.2_

  - [x]* 3.2 Write property tests for token service
    - **Property 2: Token Claims Completeness** - Generate users with random role subsets; verify JWT contains all assigned roles and claims
    - **Property 6: Refresh Token Rotation Invalidates Previous** - Generate valid tokens; refresh; verify old token is revoked
    - **Property 7: Refresh Token Reuse Detection** - Revoke token; attempt reuse; verify cascade revocation of all user tokens
    - **Property 8: Refreshed Token Reflects Current Roles** - Modify roles between issuance and refresh; verify new claims
    - **Property 14: Token Validation Rejects Tampered Tokens** - Generate valid tokens; tamper with signature/expiry/issuer; verify rejection
    - **Property 20: Remember Me Token Expiration Extension** - Generate login requests with/without RememberMe; verify 30-day vs 7-day expiration
    - **Validates: Requirements 2.1, 2.2, 4.1, 4.3, 4.5, 17.2, 17.3, 24.1, 24.2**

  - [x] 3.3 Implement CurrentUserService
    - Create `Infrastructure/Identity/CurrentUserService.cs` implementing `ICurrentUserService`
    - Read claims from `IHttpContextAccessor.HttpContext.User`
    - Expose UserId, Email, Roles, IsAuthenticated, IsInRole, HasPermission
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 3.4 Implement IdentityService
    - Create `Infrastructure/Identity/IdentityService.cs` implementing `IIdentityService`
    - Implement user registration with ASP.NET Identity UserManager
    - Implement credential validation, email confirmation token generation, password reset
    - Configure password complexity rules (min 8 chars, uppercase, lowercase, digit, special char)
    - Configure account lockout (5 attempts, 15 min duration)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.7, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x]* 3.5 Write property tests for authentication logic
    - **Property 1: Password Validation Correctness** - Generate random strings; classify as valid/invalid based on complexity rules; verify validator accepts/rejects correctly
    - **Property 3: Credential Error Message Uniformity** - Generate invalid credentials (wrong email, wrong password, both wrong); verify identical generic error response
    - **Property 4: Failed Login Counter Increment** - Generate random N (1-4) consecutive failed attempts; verify counter equals N
    - **Property 5: Locked Account Rejects All Login** - Lock account; generate valid and invalid credentials; verify all rejected
    - **Property 9: Password Reset Prevents Email Enumeration** - Generate registered and unregistered emails; verify identical success response structure
    - **Validates: Requirements 1.3, 1.4, 2.5, 3.1, 3.2, 3.3, 5.2**

  - [x] 3.6 Implement TwoFactorService
    - Create `Infrastructure/Identity/TwoFactorService.cs` implementing `ITwoFactorService`
    - Implement TOTP secret generation and QR code URI creation
    - Implement code verification using Identity's built-in TOTP validator
    - Implement recovery code generation (10 codes) and single-use validation
    - Implement enable/disable 2FA with mandatory verification before activation
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7, 22.1, 22.2, 22.3_

  - [x]* 3.7 Write property test for TOTP verification
    - **Property 17: TOTP Verification Correctness** - Generate valid/invalid TOTP codes for users with 2FA enabled; verify accept/reject behavior
    - **Validates: Requirements 21.2, 21.3, 21.4, 22.1, 22.3, 22.4**

  - [x] 3.8 Implement SessionService
    - Create `Infrastructure/Identity/SessionService.cs` implementing `ISessionService`
    - Implement session creation with device info extraction (User-Agent parsing)
    - Implement session listing, revocation (single and all), and activity tracking
    - Implement automatic session cleanup for inactive sessions beyond timeout
    - _Requirements: 24.2, 24.3, 24.4, 24.5, 24.6, 24.7_

  - [x]* 3.9 Write property test for session revocation
    - **Property 19: Session Revocation Invalidates Tokens** - Create sessions; revoke them; verify associated refresh tokens are invalidated and subsequent refresh attempts are rejected
    - **Validates: Requirements 24.4, 24.6**

- [x] 4. Checkpoint - Ensure all infrastructure services compile and unit tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement authorization policies and resource-based filtering
  - [x] 5.1 Configure authorization policies
    - Create `Infrastructure/Identity/AuthorizationPolicies.cs` with static Configure method
    - Register all named policies: CanManageUsers, CanProcessApplications, CanManageProducts, CanViewReports, CanManageLenders, CanManageBorrowers, CanVetUsers, CanApproveProducts, CanHandleDisputes, CanManageMessages, CanSetLimits, CanApproveDisbursements
    - Map each policy to its required roles as defined in the design
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.9, 8.10, 8.11, 8.12, 8.13, 8.14_

  - [x]* 5.2 Write property test for policy-based authorization
    - **Property 12: Policy-Based Endpoint Authorization** - Generate user-role-endpoint triples; verify access granted only when user holds a role listed in the endpoint's policy; verify 403 for insufficient roles and 401 for unauthenticated
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8**

  - [x] 5.3 Implement resource-based authorization behaviour
    - Create `Application/Common/Behaviours/ResourceAuthorizationBehaviour.cs` as a MediatR pipeline behaviour
    - Create `Application/Common/Interfaces/IResourceFilteredQuery.cs` interface with FilterByUserId and FilterByRole
    - Implement logic: Borrower-only users get BorrowerId filter, Lender-only users get LenderId filter, Admin/CrmManager/Auditor get no filter
    - Apply the behaviour to existing loan application and loan product queries
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

  - [x]* 5.4 Write property test for resource-based data isolation
    - **Property 13: Resource-Based Data Isolation** - Generate multi-user data with different roles; query as each role; verify Borrowers see only own resources, Lenders see only own products/applications, Admin/CrmManager/Auditor see all
    - **Validates: Requirements 9.1, 9.2, 9.3, 9.4**

  - [x] 5.5 Implement account status enforcement middleware
    - Create `Application/Common/Behaviours/AccountStatusBehaviour.cs` as a MediatR pipeline behaviour
    - Enforce: PendingApproval users can only view profile, Hold users cannot create new loans/products, Blocked users cannot perform blocked activity, Suspended users denied all access, Closed users denied all authentication
    - Return appropriate error codes (AUTH_PENDING_APPROVAL, AUTH_ACCOUNT_SUSPENDED, AUTH_ACCOUNT_CLOSED)
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5, 19.1, 19.2, 19.3, 19.4, 19.5_

  - [x]* 5.6 Write property test for account status enforcement
    - **Property 15: Account Status Enforcement** - Generate users in each AccountStatus; attempt various operations; verify access restrictions match status rules
    - **Validates: Requirements 18.2, 18.3, 18.4, 18.5, 19.1, 19.2, 19.3, 19.4, 19.5**

  - [x] 5.7 Implement credit and capital limit enforcement
    - Create `Application/Common/Behaviours/LimitEnforcementBehaviour.cs` as a MediatR pipeline behaviour
    - Enforce borrower credit limits on loan application commands
    - Enforce lender capital limits on loan product funding commands
    - Enforce maximum active loans per borrower (configurable via AccountSettings)
    - Return LIMIT_CREDIT_EXCEEDED, LIMIT_CAPITAL_EXCEEDED, LIMIT_MAX_LOANS error codes
    - _Requirements: 20.1, 20.2, 20.4, 20.5, 20.6, 20.7, 21.3, 21.4, 21.5, 21.6_

  - [x]* 5.8 Write property test for credit limit enforcement
    - **Property 16: Credit Limit Enforcement** - Generate amounts relative to assigned limits; verify applications within limits are accepted and those exceeding limits are rejected
    - **Validates: Requirements 20.1, 20.2, 20.5, 20.6, 21.3, 21.4, 21.5, 21.6**

  - [x] 5.9 Implement granular permission resolver
    - Create `Infrastructure/Identity/PermissionResolver.cs` that computes effective permissions as union of all assigned roles (predefined + custom)
    - Integrate with `ICurrentUserService.HasPermission` to check module+action combinations
    - Implement permission simulation query for admin testing tool
    - _Requirements: 23.1, 23.2, 23.3, 23.4, 23.6, 23.8_

  - [x]* 5.10 Write property test for permission union
    - **Property 18: Permission Union Across Multiple Roles** - Generate users with multiple roles; verify effective permission set equals union of all role permissions; if any role grants a permission, user has it
    - **Validates: Requirements 23.4, 23.8**

- [x] 6. Checkpoint - Ensure authorization behaviours and policies compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Implement CQRS commands and queries for authentication
  - [x] 7.1 Implement RegisterCommand handler
    - Create `Application/Features/Auth/Commands/Register/RegisterCommand.cs` and `RegisterCommandHandler.cs`
    - Validate email format and password complexity
    - Create ApplicationUser via IIdentityService, assign Borrower/Lender role based on UserType
    - Set AccountStatus to PendingApproval, send email verification link
    - Record audit log entry for registration
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.11_

  - [x] 7.2 Implement LoginCommand handler
    - Create `Application/Features/Auth/Commands/Login/LoginCommand.cs` and `LoginCommandHandler.cs`
    - Validate credentials via IIdentityService, check account status (reject disabled/suspended/closed/pending)
    - Handle 2FA flow: if 2FA enabled, require TotpCode; if mandatory 2FA not configured, redirect to setup
    - Generate tokens via ITokenService with RememberMe flag
    - Create session via ISessionService, record audit log for login
    - Return generic error for invalid credentials (no email enumeration)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 3.1, 3.2, 3.3, 3.4, 3.5, 21.3, 21.4, 21.5, 24.1_

  - [x] 7.3 Implement RefreshTokenCommand and LogoutCommand handlers
    - Create `Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs` and handler
    - Implement token rotation: validate refresh token, issue new pair, revoke old token
    - Implement reuse detection: if revoked token presented, revoke all user tokens
    - Create `Application/Features/Auth/Commands/Logout/LogoutCommand.cs` and handler
    - Revoke refresh token and terminate session on logout
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 24.5_

  - [x] 7.4 Implement password reset commands
    - Create `Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` and handler
    - Generate time-limited reset token (1 hour), send email for registered users, return success for all (prevent enumeration)
    - Create `Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommand.cs` and handler
    - Validate token, update password, revoke all refresh tokens, record audit log
    - Create `Application/Features/Auth/Commands/ConfirmEmail/ConfirmEmailCommand.cs` and handler
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 1.8, 1.9_

  - [x] 7.5 Implement 2FA commands
    - Create `Application/Features/Auth/Commands/TwoFactor/Setup2FaCommand.cs` and handler (generate secret, return QR URI)
    - Create `Application/Features/Auth/Commands/TwoFactor/Verify2FaCommand.cs` and handler (verify code, enable 2FA, generate recovery codes)
    - Create `Application/Features/Auth/Commands/TwoFactor/Disable2FaCommand.cs` and handler
    - Record audit log for 2FA enable/disable events
    - _Requirements: 21.1, 21.2, 21.6, 21.7, 21.8, 21.9, 21.10, 22.2, 22.3, 22.7_

- [x] 8. Implement CQRS commands and queries for user management
  - [x] 8.1 Implement user management commands
    - Create `Application/Features/Users/Commands/CreateUser/CreateUserCommand.cs` and handler
    - Create `Application/Features/Users/Commands/UpdateUser/UpdateUserCommand.cs` and handler
    - Create `Application/Features/Users/Commands/ChangeAccountStatus/ChangeAccountStatusCommand.cs` and handler
    - Enforce mandatory reason for status changes, send email/in-app notifications
    - Prevent status change that would remove last Active Admin
    - Record audit log for all user management actions
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 18.6, 18.7, 18.8, 18.9, 18.10, 19.6, 19.7, 19.8, 19.9, 19.10_

  - [x] 8.2 Implement role management commands
    - Create `Application/Features/Users/Commands/AssignRole/AssignRoleCommand.cs` and handler
    - Create `Application/Features/Users/Commands/RemoveRole/RemoveRoleCommand.cs` and handler
    - Prevent removal of last Admin role from the system
    - Record audit log for role assignment/removal
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x]* 8.3 Write property tests for role management
    - **Property 10: Role Assignment Round-Trip** - Generate role subsets; assign then query to verify presence; remove then query to verify absence; verify multiple simultaneous roles
    - **Property 11: Non-Admin Role Management Rejected** - Generate non-Admin users; attempt role assignment/removal; verify 403 Forbidden
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.5**

  - [x] 8.4 Implement vetting workflow commands
    - Create `Application/Features/Vetting/Commands/ApproveRegistration/ApproveRegistrationCommand.cs` and handler
    - Require CreditTier and CreditLimit for Borrower approval, CapitalLimit for Lender approval
    - Create `Application/Features/Vetting/Commands/RejectRegistration/RejectRegistrationCommand.cs` and handler
    - Create `Application/Features/Vetting/Commands/RequestDocuments/RequestDocumentsCommand.cs` and handler
    - Set account status to DocumentsRequested, send notification to applicant
    - Record audit log for all vetting decisions, notify Admin of decisions
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6, 22.7, 22.8, 22.9, 20.1, 20.3, 20.4, 20.5, 20.6, 20.7, 20.8, 20.9_

  - [x] 8.5 Implement credit and limits commands
    - Create `Application/Features/Credit/Commands/SetCreditTier/SetCreditTierCommand.cs` and handler
    - Create `Application/Features/Credit/Commands/SetCreditLimit/SetCreditLimitCommand.cs` and handler
    - Create `Application/Features/Credit/Commands/SetCapitalLimit/SetCapitalLimitCommand.cs` and handler
    - Require justification for all limit changes, record audit log with previous/new values
    - _Requirements: 19.1, 19.2, 19.5, 19.6, 19.7, 19.8, 19.9, 20.1, 20.2, 20.3, 20.8, 20.9, 21.1, 21.2, 21.7_

  - [x] 8.6 Implement custom role commands
    - Create `Application/Features/Roles/Commands/CreateCustomRole/CreateCustomRoleCommand.cs` and handler
    - Create `Application/Features/Roles/Commands/UpdateCustomRole/UpdateCustomRoleCommand.cs` and handler
    - Create `Application/Features/Roles/Commands/DeleteCustomRole/DeleteCustomRoleCommand.cs` and handler
    - Prevent deletion of predefined system roles (Admin, CrmManager, CustomerService, Lender, Borrower, Auditor)
    - Record audit log for custom role CRUD operations
    - _Requirements: 23.1, 23.3, 23.6, 23.7, 23.9_

  - [x] 8.7 Implement session management commands
    - Create `Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommand.cs` and handler
    - Create `Application/Features/Sessions/Commands/RevokeAllSessions/RevokeAllSessionsCommand.cs` and handler
    - Invalidate associated refresh tokens when sessions are revoked
    - _Requirements: 24.4, 24.5, 24.6, 24.7_

  - [x] 8.8 Implement queries
    - Create `Application/Features/Users/Queries/GetUsers/GetUsersQuery.cs` and handler with pagination, search, role filter
    - Create `Application/Features/Users/Queries/GetUserById/GetUserByIdQuery.cs` and handler
    - Create `Application/Features/Vetting/Queries/GetVettingQueue/GetVettingQueueQuery.cs` and handler (PendingApproval users sorted by date)
    - Create `Application/Features/Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs` and handler
    - Create `Application/Features/Roles/Queries/GetRoles/GetRolesQuery.cs` and handler
    - Create `Application/Features/Roles/Queries/GetRolePermissions/GetRolePermissionsQuery.cs` and handler
    - Create `Application/Features/Roles/Queries/SimulatePermissions/SimulatePermissionsQuery.cs` and handler
    - Create `Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs` and handler
    - _Requirements: 14.1, 22.9, 23.5, 24.2, 24.3_

- [x] 9. Checkpoint - Ensure all CQRS handlers compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Implement API controllers and startup configuration
  - [x] 10.1 Implement AuthController
    - Create `Controllers/AuthController.cs` with endpoints: POST register, login, refresh-token, logout, forgot-password, reset-password, confirm-email, 2fa/setup, 2fa/verify, 2fa/disable
    - Apply [Authorize] to logout, 2fa/setup, 2fa/verify, 2fa/disable endpoints
    - Dispatch to MediatR commands/queries
    - _Requirements: 1.1, 2.1, 4.1, 5.1, 21.1, 21.2_

  - [x] 10.2 Implement UserManagementController and VettingController
    - Create `Controllers/UserManagementController.cs` with [Authorize(Policy = "CanManageUsers")]
    - Endpoints: GET users, GET user by id, POST create, PUT update, POST status change, POST assign-role, POST remove-role, GET sessions, POST revoke session
    - Create `Controllers/VettingController.cs` with [Authorize(Policy = "CanVetUsers")]
    - Endpoints: GET queue, POST approve, POST reject, POST request-docs
    - _Requirements: 7.5, 8.1, 8.9, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 22.3, 22.4, 22.5_

  - [x] 10.3 Implement RoleController, CreditController, and SessionController
    - Create `Controllers/RoleController.cs` with [Authorize(Policy = "CanManageUsers")]
    - Endpoints: GET roles, GET permissions, POST create, PUT update, DELETE, POST simulate
    - Create `Controllers/CreditController.cs` with [Authorize(Policy = "CanSetLimits")]
    - Endpoints: POST tier, POST credit-limit, POST capital-limit
    - Create `Controllers/SessionController.cs` with [Authorize]
    - Endpoints: GET my sessions, POST revoke
    - _Requirements: 8.13, 19.5, 23.1, 24.3, 24.4_

  - [x] 10.4 Configure authentication and authorization in Program.cs
    - Register ASP.NET Identity with AuthIdentityDbContext
    - Configure JWT Bearer authentication with validation parameters
    - Register authorization policies via AuthorizationPolicies.Configure
    - Register all infrastructure services (ITokenService, ICurrentUserService, IIdentityService, ITwoFactorService, ISessionService)
    - Configure CORS for Blazor WASM origin (production: specific origin, dev: configurable)
    - Add JwtSettings, AccountSettings configuration binding
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 17.1, 17.2, 17.3, 17.4_

  - [x] 10.5 Implement IdentitySeeder
    - Create `Infrastructure/Identity/IdentitySeeder.cs`
    - Seed predefined roles (Admin, CrmManager, CustomerService, Lender, Borrower, Auditor) on startup
    - Create default Admin account from appsettings (AdminSeed:Email, AdminSeed:Password)
    - Associate roles with policy claims
    - Register seeder to run on application startup
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10_

  - [x] 10.6 Add [Authorize] attributes to existing controllers
    - Add [Authorize(Policy = "CanManageBorrowers")] to BorrowersController
    - Add [Authorize(Policy = "CanManageLenders")] to LendersController (Admin only for creation)
    - Add [Authorize(Policy = "CanProcessApplications")] to LoanApplicationsController workflow endpoints
    - Add [Authorize(Policy = "CanManageProducts")] to LoanProductsController
    - Add [Authorize(Policy = "CanViewReports")] to AuditLogsController and DashboardController
    - Ensure unauthenticated requests return 401, insufficient permissions return 403
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

  - [x]* 10.7 Write integration tests for API authentication and authorization
    - Test full login → token → API access → refresh → logout flow
    - Test CORS configuration (allowed/rejected origins)
    - Test JWT configuration validation (short key rejection at startup)
    - Test policy enforcement on controller endpoints (401/403 responses)
    - Test resource-based filtering with real EF Core queries
    - _Requirements: 2.1, 4.1, 8.7, 8.8, 16.2, 17.5_

- [x] 11. Checkpoint - Ensure API layer compiles and integration tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 12. Implement Blazor WebAssembly authentication integration
  - [x] 12.1 Implement JwtAuthenticationStateProvider
    - Create `Services/Auth/JwtAuthenticationStateProvider.cs` extending `AuthenticationStateProvider`
    - Read JWT from localStorage, parse claims to build ClaimsPrincipal
    - Monitor token expiration, trigger refresh 2 minutes before expiry
    - Notify subscribers on auth state changes (login/logout)
    - Handle expired tokens by attempting refresh before reporting unauthenticated
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 13.1, 13.2, 13.3_

  - [x] 12.2 Implement AuthApiClient and AuthTokenHandler
    - Create `Services/Auth/AuthApiClient.cs` with LoginAsync, RefreshTokenAsync, RegisterAsync, LogoutAsync, ForgotPasswordAsync, ResetPasswordAsync
    - Create `Services/Auth/AuthTokenHandler.cs` as DelegatingHandler
    - Attach Bearer token to all outgoing requests from Authorization header
    - Intercept 401 responses, attempt token refresh, retry original request
    - Redirect to login on refresh failure
    - _Requirements: 13.4, 10.1, 10.4, 10.5_

  - [x] 12.3 Implement authentication pages
    - Create `Pages/Auth/Login.razor` with email, password, RememberMe checkbox, 2FA code input (conditional)
    - Create `Pages/Auth/Register.razor` with role-specific registration flows (Borrower/Lender)
    - Create `Pages/Auth/ForgotPassword.razor` and `Pages/Auth/ResetPassword.razor`
    - Create `Pages/Auth/TwoFactorSetup.razor` (QR code display, verification input)
    - Create `Pages/Auth/TwoFactorVerify.razor` (TOTP code input during login)
    - Store tokens in localStorage on successful login, notify AuthStateProvider
    - _Requirements: 1.10, 1.11, 10.4, 10.5, 21.2, 24.1_

  - [x] 12.4 Implement protected routes and role-based UI rendering
    - Configure `AuthorizeRouteView` in App.razor for route-level authorization
    - Implement redirect to login for unauthenticated users with return URL preservation
    - Implement access denied page for insufficient role
    - Create role-based navigation component using `AuthorizeView` with Policy attributes
    - Admin: all nav items; CrmManager: vetting queue, borrowers, lenders, credit; CustomerService: disputes, messages, FAQ; Lender: own products, applications; Borrower: own applications, payments; Auditor: read-only all
    - Hide workflow action buttons from users without corresponding policy
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7_

  - [ ] 12.5 Implement session management page
    - Create `Pages/Account/Sessions.razor` displaying active sessions with device, IP, browser, last activity
    - Implement session revocation (single session and all sessions)
    - _Requirements: 24.3, 24.4_

  - [ ] 12.6 Implement admin user management pages
    - Create `Pages/Admin/UserManagement.razor` with user list, search, role filter, pagination
    - Create `Pages/Admin/VettingQueue.razor` with pending registrations, approve/reject/request-docs actions
    - Create `Pages/Admin/RoleManagement.razor` with custom role CRUD and permission assignment
    - Implement permission simulation tool UI
    - _Requirements: 14.1, 22.9, 23.3, 23.5_

  - [ ]* 12.7 Write unit tests for Blazor authentication components
    - Test JwtAuthenticationStateProvider token parsing and state management
    - Test AuthTokenHandler 401 interception and retry logic
    - Test role-based navigation rendering for each role
    - Test protected route redirect behavior
    - _Requirements: 10.1, 10.2, 10.3, 11.1, 11.2, 13.1, 13.3_

- [ ] 13. Implement audit logging for authentication events
  - [ ] 13.1 Implement authentication audit logging
    - Extend existing AuditLog infrastructure to record auth events
    - Log successful logins (EntityName: "ApplicationUser", Action: "Login", include email and IP)
    - Log failed login attempts (Action: "LoginFailed", include attempted email and IP)
    - Log account lockouts (Action: "AccountLocked", include email and lockout duration)
    - Log password resets (Action: "PasswordReset", include email)
    - Log role changes (Action: "RoleChanged", include target email, role name, added/removed)
    - Log account status changes (Action: "AccountStatusChanged", include email and new status)
    - Log 2FA enable/disable events
    - Log vetting decisions (approve, reject, request docs)
    - Log credit tier and limit changes
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 18.7, 19.9, 20.9, 21.10, 22.7, 23.7_

- [ ] 14. Final checkpoint - Ensure all tests pass and project builds successfully
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at logical boundaries
- Property tests use FsCheck.Xunit with minimum 100 iterations per property
- The implementation follows the migration strategy: Schema → Seeding → Link Entities → Protect Endpoints → Blazor Integration
- Both `ApplicationDbContext` (domain) and `AuthIdentityDbContext` (identity) target the same SQL Server database
- Existing controllers gain `[Authorize]` attributes incrementally to maintain backward compatibility during development
- The `UserId` FK on Borrower/Lender is nullable to support existing records without linked user accounts

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.4", "1.5"] },
    { "id": 2, "tasks": ["1.3"] },
    { "id": 3, "tasks": ["3.1", "3.3", "3.4"] },
    { "id": 4, "tasks": ["3.2", "3.5", "3.6", "3.8"] },
    { "id": 5, "tasks": ["3.7", "3.9", "5.1", "5.3", "5.5", "5.7", "5.9"] },
    { "id": 6, "tasks": ["5.2", "5.4", "5.6", "5.8", "5.10"] },
    { "id": 7, "tasks": ["7.1", "7.2", "7.3", "7.4", "7.5"] },
    { "id": 8, "tasks": ["8.1", "8.2", "8.4", "8.5", "8.6", "8.7", "8.8"] },
    { "id": 9, "tasks": ["8.3"] },
    { "id": 10, "tasks": ["10.1", "10.2", "10.3", "10.4", "10.5"] },
    { "id": 11, "tasks": ["10.6", "10.7"] },
    { "id": 12, "tasks": ["12.1", "12.2"] },
    { "id": 13, "tasks": ["12.3", "12.4", "12.5", "12.6"] },
    { "id": 14, "tasks": ["12.7", "13.1"] }
  ]
}
```
