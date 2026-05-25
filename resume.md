# Resume — Project Context for AI Assistant

## How to Use This File

Give this file to the AI assistant at the start of any new session:
"Read resume.md and continue working on this project."

The assistant should then read the key files referenced below to rebuild context.

---

## Project Identity

- **Name:** Loan Investment Supermarket
- **Type:** Peer-to-peer lending marketplace
- **Stack:** ASP.NET Core 10, Blazor WASM, EF Core, SQL Server, MediatR, Tailwind CSS + DaisyUI (Corporate theme)
- **Architecture:** Clean Architecture + CQRS + DDD
- **Solution file:** `LoanSuperMarketUsingBlazor.slnx`
- **Database:** `Server=DESKTOP-VVJN96B;Database=LoanSuperMarketDb`

---

## Solution Structure

```
src/
├── LoanSuperMarket.Api/            ← ASP.NET Core Web API
├── LoanSuperMarket.Application/    ← CQRS handlers, services, behaviours
├── LoanSuperMarket.Domain/         ← Entities, value objects, domain services
├── LoanSuperMarket.Infrastructure/ ← EF Core, repos, Dapper, identity, background services
├── LoanSuperMarket.Shared/         ← DTOs shared between API and Blazor
└── LoanSuperMarket.Blazor/         ← Blazor WebAssembly frontend

tests/
├── LoanSuperMarket.Domain.Tests/
├── LoanSuperMarket.Application.Tests/
└── LoanSuperMarket.Api.Tests/

docs/                               ← 11 comprehensive documentation files
onboarding/                         ← 34 deep-dive feature guides for developers
LoanSuperMarketScreens/            ← Screenshots (admin/, borrowers/, lenders/)
```

---

## Key Files to Read for Context

| Purpose | File |
|---------|------|
| Full progress report | `ProjectProgress.md` |
| Architecture & patterns | `docs/03-architecture-overview.md` |
| Domain entities | `src/LoanSuperMarket.Domain/Entities/` (all files) |
| Main handler | `src/LoanSuperMarket.Application/Features/Funding/FundLoan/FundLoanCommandHandler.cs` |
| API startup | `src/LoanSuperMarket.Api/Program.cs` |
| DI registration | `src/LoanSuperMarket.Infrastructure/DependencyInjection.cs` |
| Blazor layout | `src/LoanSuperMarket.Blazor/Layout/MainLayout.razor` |
| Blazor DI | `src/LoanSuperMarket.Blazor/Program.cs` |
| Database context | `src/LoanSuperMarket.Infrastructure/Persistence/ApplicationDbContext.cs` |
| Test data | `src/LoanSuperMarket.Infrastructure/Persistence/DevelopmentDataSeeder.cs` |
| Identity seeder | `src/LoanSuperMarket.Infrastructure/Identity/IdentitySeeder.cs` |
| JWT token service | `src/LoanSuperMarket.Infrastructure/Identity/JwtTokenService.cs` |
| Tailwind config | `src/LoanSuperMarket.Blazor/tailwind.config.js` |

---

## Demo Accounts (All Seeded on First Startup)

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@loansupermarket.com` | `Admin@123456!` |
| Admin | `admin2@demo.com` | `Demo@12345!` |
| CRM Manager | `crm1@demo.com` | `Demo@12345!` |
| CRM Manager | `crm2@demo.com` | `Demo@12345!` |
| Customer Service | `staff1@demo.com` | `Demo@12345!` |
| Customer Service | `staff2@demo.com` | `Demo@12345!` |
| Lender | `lender1@demo.com` - `lender5@demo.com` | `Demo@12345!` |
| Borrower | `borrower1@demo.com` - `borrower5@demo.com` | `Demo@12345!` |

---

## What's Been Built (Complete Features)

### Core Lending Lifecycle (End-to-End)
- Borrower applies via wizard → CRM reviews → Approves → Lender funds → Monthly repayments → Loan complete
- Document upload, verification, rejection
- Amortization engine (EMI formula with rounding correction)
- Payment processing (single, partial, bulk/early payoff)
- Late payment detection (background service, grace period, late fees)
- Default detection (3+ consecutive missed)
- Loan restructuring for distressed loans

### Dashboards
- Admin: KPI cards, conversion metrics, recent applications, quick actions, new borrowers
- Lender: Portfolio, Loans, Earnings, Analytics (ROI/yield/diversification), Product Comparison
- Borrower: Applications, Active Loans, Payment History, Upcoming Payments, Calculator

### Infrastructure
- JWT auth with refresh tokens, token rotation, reuse detection
- AddIdentityCore (NOT AddIdentity — prevents cookie scheme override)
- MapInboundClaims=false, RoleClaimType="role", OnTokenValidated identity fix
- 6 roles with granular module-level permissions
- SignalR real-time notifications (hub + client connected)
- Background hosted service for daily late payment processing
- Stored procedures + Dapper for reporting
- IMemoryCache with cache-aside pattern (CachingBehaviour)
- Rate limiting (100 req/min per IP)
- Health checks (/health)
- Correlation ID middleware
- Domain events (MediatR INotification)
- Specification pattern
- 30+ reusable Blazor components
- DaisyUI Corporate theme
- CSV export
- Error boundary
- Development data seeder (comprehensive: 5 lenders, 5 borrowers, 20 products, 15 applications, 6 funded loans)

### Testing
- 30 unit tests (Domain + Application)
- xUnit + Moq framework
- Tests cover: Lender, Installment state machine, PaymentProcessor, AmortizationService

### Documentation
- 11 comprehensive docs in `docs/` folder
- 34 deep-dive onboarding documents in `onboarding/` folder
- Professional README with embedded screenshots
- Screenshots in `LoanSuperMarketScreens/` (admin, borrowers, lenders)

---

## Critical Technical Decisions (Don't Change These)

### JWT Authentication Setup
```
- Uses AddIdentityCore (NOT AddIdentity) to prevent cookie scheme override
- Token generation: claims use "role" claim type
- ClaimsIdentity created with: new ClaimsIdentity(claims, "jwt", "email", "role")
- OutboundClaimTypeMap.Clear() on JwtSecurityTokenHandler
- API validation: MapInboundClaims = false, RoleClaimType = "role"
- OnTokenValidated event re-creates ClaimsIdentity with correct role type
```

### DashboardController Authorization
```
- Class-level: [Authorize] (just requires authentication)
- Per-endpoint: [Authorize(Roles = "...")] for specific role access
- Lender endpoints: "Lender,Admin"
- Borrower endpoints: "Borrower,Admin"
- Admin endpoints: "Admin"
```

### Registration Flow
```
- Email auto-confirmed (no email provider configured)
- Account status set to Active immediately (no vetting in dev mode)
- Borrower/Lender entities linked to Identity users via UserId
```

### Seeder Dependency Order
```
1. Users (Identity) → SaveChanges
2. Lenders + Borrowers → SaveChanges
3. Link UserId to Lenders/Borrowers → SaveChanges
4. Loan Products → SaveChanges
5. Loan Applications → SaveChanges
6. Repayment Schedules + Payments → SaveChanges
7. Audit Logs → SaveChanges
```

---

## What's NOT Done Yet (Roadmap)

### High Priority
- [ ] Wire SignalR events into Blazor pages (auto-refresh on FundingQueueChanged)
- [ ] Email notifications via SendGrid (replace NoOpEmailService)
- [ ] API versioning attributes on controllers
- [ ] Integration tests with WebApplicationFactory

### Medium Priority
- [ ] Loan performance charts (line chart over time)
- [ ] Secondary market (lenders sell loan positions)
- [ ] Auto-invest rules (lenders set criteria, platform auto-funds)
- [ ] Collections workflow actions (contact, payment plan, write-off)
- [ ] Notification preferences persistence (currently logs only)
- [ ] Mobile-responsive layout polish

### Low Priority
- [ ] Azure deployment (App Services + SQL Azure)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Application Insights + structured logging
- [ ] Property-based testing for amortization invariants
- [ ] Multi-currency support
- [ ] Regulatory reporting

---

## Known Issues / Technical Debt

1. **LatePaymentService** loads ALL active schedules into memory. For large platforms, needs pagination or batching.
2. **Dashboard handlers** for lender earnings/analytics iterate schedules in memory. Could use stored procedures for better performance at scale.
3. **Notification preferences** are not persisted to a database table — currently just logged. Needs a `NotificationPreferences` table.
4. **Collections page** queries all lenders then all their schedules. Needs a dedicated optimized query.
5. **Tailwind CSS** must be rebuilt manually after adding new utility classes: `npx tailwindcss -i wwwroot/css/tailwind-input.css -o wwwroot/css/app.css` (from Blazor project folder).

---

## Conventions to Follow

- **Commands:** `sealed record XxxCommand(...) : IRequest<ApiResponse<T>>`
- **Handlers:** `sealed class XxxCommandHandler : IRequestHandler<XxxCommand, ApiResponse<T>>`
- **Validators:** `sealed class XxxCommandValidator : AbstractValidator<XxxCommand>`
- **Entities:** Private constructors + static `Create()` factory + `MarkUpdated()` on every mutation
- **DTOs:** In `Shared/` project, public get/set, organized by domain folder
- **API responses:** Always `ApiResponse<T>` wrapper
- **Blazor pages:** `@attribute [Authorize]`, inject ApiClient + ToastService, LoadingSkeleton + EmptyState pattern
- **Tests:** xUnit, one test class per entity/service, descriptive method names

---

## How to Run

```bash
# API (from solution root)
cd src/LoanSuperMarket.Api
dotnet run

# Blazor (separate terminal)
cd src/LoanSuperMarket.Blazor
dotnet run
```

**API:** `https://localhost:7117` (Swagger at `/swagger`)
**Blazor:** `http://localhost:5036`

The `DevelopmentDataSeeder` runs on first startup and creates all demo data.

### If You Need to Reset the Database:
```bash
cd C:\Users\afzal\source\repos\LoanSuperMarketUsingBlazor
dotnet ef database drop --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context ApplicationDbContext --force
dotnet ef database drop --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context AuthIdentityDbContext --force
dotnet ef database update --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context ApplicationDbContext
dotnet ef database update --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context AuthIdentityDbContext
```

### If You Need to Rebuild Tailwind CSS:
```bash
cd src\LoanSuperMarket.Blazor
npx tailwindcss -i wwwroot/css/tailwind-input.css -o wwwroot/css/app.css
```

---

## Last Session Summary

In the most recent session we:
1. Fixed JWT authentication (AddIdentityCore, role claim mapping, OnTokenValidated)
2. Fixed DashboardController authorization (per-endpoint roles instead of restrictive policy)
3. Created 5 missing pages (Payments, Disputes, Messages, Notifications, AuditLogs)
4. Fixed nav menu roles and broken links
5. Fixed Swagger error (WizardController IFormFile)
6. Switched to DaisyUI Corporate theme
7. Redesigned Admin Dashboard with KPI cards, metrics, activity feed
8. Fixed DevelopmentDataSeeder (CreateDraft flow, dependency ordering, user linking)
9. Auto-confirm email and auto-activate accounts on registration
10. Fixed Blazor file upload (buffer stream before StateHasChanged)
11. Fixed wizard state (carry data to Review step)
12. Created comprehensive seeder (5 lenders, 5 borrowers, 20 products, 15 apps, 6 funded)
13. Rewrote README.md with screenshots and professional narrative
14. Created 34 onboarding documents (~17,000+ lines of developer documentation)
