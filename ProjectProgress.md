# Project Progress — Loan Investment Supermarket

## What Are We Building?

A peer-to-peer lending marketplace. Think of it like a stock exchange, but for loans. Lenders put up capital, borrowers apply for loans, and the platform matches them, handles the money flow, tracks repayments, and manages the entire lifecycle.

It's not a demo or a tutorial — it's a production-grade financial platform built with enterprise architecture patterns.

---

## What's Working Right Now

### The Full Lending Lifecycle

A loan goes through this journey on our platform:

```
Borrower applies → CRM reviews → Approved → Lender funds it → Monthly repayments → Loan complete
```

Every step of that is implemented, end to end, across backend and frontend.

### For Borrowers
- Apply for loans through a guided wizard
- Get matched with suitable loan products
- Upload documents for verification
- View active loans with progress bars
- Make payments (single or pay-off-entire-loan)
- See payment history and upcoming schedule
- Get warnings when payments are due soon

### For Lenders
- Browse a funding queue of approved applications
- See borrower credit tier, amount, rate, and purpose
- Accept funding (auto-generates amortization schedule, deducts capital)
- Decline with reason
- Top up capital when funds run low
- Restructure distressed loans (extend term, adjust rate)
- View portfolio: active loans, earnings, default rate
- Investor analytics: ROI per loan, annualized yield, diversification score

### For Admins
- Platform-wide loan overview across all lenders
- Filter by performance status or lender
- See default rates, outstanding principal, total funded
- Drill into any loan's repayment schedule
- User management and vetting queue

### The Engine Under the Hood
- **Amortization calculator** — EMI formula with rounding correction
- **Payment processor** — enforces sequential payment order, handles partial/full/bulk payments
- **Late payment detection** — background service runs daily, applies late fees after grace period
- **Default detection** — 3+ consecutive missed = defaulted, notifications sent
- **Credit tier rate adjustment** — Tier A (base), B (+2%), C (+4%)
- **Audit trail** — every funding, payment, restructuring logged with timestamps

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 10 |
| Architecture | Clean Architecture + CQRS + MediatR |
| Database | SQL Server + EF Core |
| Auth | ASP.NET Identity + JWT + 2FA |
| Real-time | SignalR |
| Frontend | Blazor WebAssembly |
| Styling | Tailwind CSS |
| Validation | FluentValidation + DataAnnotations |

---

## Architecture Decisions

- **Domain-Driven Design** — entities have behaviour, not just data. State machines protect invariants.
- **CQRS** — commands change state, queries read it. Never mixed.
- **Pipeline behaviours** — validation, logging, limit enforcement, resource authorization all happen automatically in the MediatR pipeline.
- **Resource-scoped queries** — lenders only see their loans, borrowers only see theirs. Enforced at the query level.
- **Background processing** — late payment detection runs on a timer, not triggered by user actions.
- **Layered validation** — domain guards → FluentValidation → API model validation → Blazor form validation.

---

## What We Built (Chronological)

### Phase 1 — Foundation
- Clean Architecture solution structure
- Domain entities: LoanProduct, Borrower, Lender, LoanApplication
- Value Objects: Money, InterestRate
- CQRS handlers for all CRUD operations
- EF Core persistence with full configurations
- JWT authentication with refresh tokens
- Role-based authorization (Admin, Lender, Borrower, CrmManager)
- Permission system with module/action granularity

### Phase 2 — Operational Workflows
- Loan product lifecycle: Draft → PendingApproval → Approved → Published → Archived
- Application workflow: Draft → Submitted → UnderReview → Approved/Rejected → Funded
- Document upload and verification
- Review queue for CRM managers
- Application wizard for borrowers
- Product matching engine

### Phase 3 — Enterprise Frontend
- Reusable DataGrid infrastructure (sort, filter, page, search)
- Modal orchestration system
- Toast notification infrastructure
- Drawer quick-view panels
- Reusable form components
- Server-side grid queries
- Dashboard with KPI metrics

### Phase 4 — Funding & Repayment Engine (Current)
- Funding queue with filters and capital display
- Funding decision modal (accept/decline)
- Amortization schedule generation (EMI formula)
- Installment state machine (Pending → Paid/Late/Missed)
- Payment processing (single, partial, bulk/early payoff)
- Late payment background service with grace period
- Default detection (3+ consecutive missed)
- Notification hooks (stub — ready for email/SMS)
- Lender dashboard: portfolio, loans, earnings, analytics
- Borrower dashboard: active loans, payment history, upcoming payments
- Lender capital top-up
- Loan restructuring for distressed loans
- Investor analytics: ROI, yield, diversification score
- Admin operations panel: platform-wide loan monitoring
- CSV export of repayment schedules
- SignalR real-time notifications infrastructure
- Audit trail timeline on loan pages
- Development data seeder for demo/testing

---

## What's Next

### Immediate (next sprint)
- [ ] Wire SignalR client into Blazor pages (auto-refresh funding queue on events)
- [ ] Email notifications via SendGrid (replace stub)
- [ ] PDF export of repayment schedules (proper formatted document)
- [ ] Restructuring UI on the repayment schedule page (lender action button)

### Short-term
- [ ] Loan performance charts (line chart of payments over time)
- [ ] Lender comparison view (which products perform best)
- [ ] Borrower credit score simulation (what-if scenarios)
- [ ] Notification preferences (email/SMS/in-app toggles)
- [ ] Mobile-responsive layout polish

### Medium-term
- [ ] Secondary market — lenders can sell loan positions to other lenders
- [ ] Auto-invest rules — lenders set criteria, platform auto-funds matching loans
- [ ] Collections workflow — structured process for defaulted loans
- [ ] Regulatory reporting — generate compliance reports
- [ ] Multi-currency support

### Infrastructure
- [ ] Azure deployment (App Services + SQL Azure)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Application Insights + structured logging
- [ ] Load testing
- [ ] Property-based testing for amortization invariants

---

## Key Metrics (Code)

| Metric | Count |
|--------|-------|
| Domain entities | 8 |
| CQRS handlers | 25+ |
| API controllers | 10 |
| Blazor pages | 12 |
| Blazor components | 20+ |
| FluentValidation validators | 8 |
| Pipeline behaviours | 6 |
| Background services | 1 |
| SignalR hubs | 1 |

---

## How to Run It

```bash
# Backend
cd src/LoanSuperMarket.Api
dotnet run

# Frontend (separate terminal)
cd src/LoanSuperMarket.Blazor
dotnet run
```

Default admin: `admin@loansupermarket.com` / `Admin@123456!`

The dev seeder auto-creates sample lenders, borrowers, products, and a funded loan on first run.

---

## The Philosophy

We build features end-to-end. Every feature touches all layers — domain logic, application handlers, API endpoints, and Blazor UI. We don't leave half-built backends without frontends or vice versa.

We prioritise correctness over speed. State machines protect invariants. Sequential payment enforcement prevents data corruption. Validation happens at every layer.

We build for the next developer. Clean separation, consistent patterns, typed contracts between layers. If you understand one feature, you understand them all.
