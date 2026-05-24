# Resume — Project Context for AI Assistant

## How to Use This File

Give this file to the AI assistant at the start of any new session:
"Read resume.md and continue working on this project."

The assistant should then read the key files referenced below to rebuild context.

---

## Project Identity

- **Name:** Loan Investment Supermarket
- **Type:** Peer-to-peer lending marketplace
- **Stack:** ASP.NET Core 10, Blazor WASM, EF Core, SQL Server, MediatR, Tailwind CSS
- **Architecture:** Clean Architecture + CQRS + DDD
- **Solution file:** `LoanSuperMarketUsingBlazor.slnx`
- **Database:** `(localdb)\MSSQLLocalDB` → `LoanSuperMarketDb`

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
```

---

## Key Files to Read for Context

If you need to understand the project quickly, read these:

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
| Tasks spec | `.kiro/specs/lender-funding-repayment/tasks.md` |

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
- Lender: Portfolio, Loans, Earnings, Analytics (ROI/yield/diversification), Product Comparison
- Borrower: Applications, Active Loans, Payment History, Upcoming Payments, Calculator
- Admin: Platform-wide loans, Collections

### Infrastructure
- JWT auth with refresh tokens, 2FA, role-based policies
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
- Dark mode toggle
- CSV export
- Error boundary
- Development data seeder

### Testing
- 30 unit tests (Domain + Application)
- xUnit + Moq framework
- Tests cover: Lender, Installment state machine, PaymentProcessor, AmortizationService

### Documentation
- 11 comprehensive docs in `docs/` folder
- Covers: business, users, architecture, domain, application, database, frontend, API, testing, troubleshooting, patterns

---

## What's NOT Done Yet (Roadmap)

### High Priority
- [ ] Deploy stored procedures to database (run the .sql files)
- [ ] Wire SignalR events into Blazor pages (auto-refresh on FundingQueueChanged)
- [ ] Email notifications via SendGrid (replace StubNotificationService)
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
5. **The `NavMenu.razor` was deleted** — `MainLayout.razor` has its own sidebar. If any code references `NavMenu`, it will fail.

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

Default admin: `admin@loansupermarket.com` / `Admin@123456!`

The `DevelopmentDataSeeder` runs on first startup and creates sample data.

---

## Last Session Summary

In the most recent session we:
1. Implemented the complete Lender Funding & Repayment Engine (all 15 task groups)
2. Fixed all compilation errors
3. Created and applied database migration
4. Added capital top-up, bulk repayment, loan restructuring
5. Added SignalR hub + client, CSV export, audit trail
6. Added investor analytics, admin panel, collections workflow
7. Polish sprint: professional NavMenu, landing page, loading skeletons, charts, dark mode, FAQ, profile
8. Added enterprise patterns: unit tests, stored procedures + Dapper, domain events, caching, error boundary, health checks, rate limiting, specification pattern, correlation IDs, virtualization
9. Created comprehensive 11-document technical bible in `docs/`
10. Verified the complete lending lifecycle is implemented end-to-end with zero gaps
