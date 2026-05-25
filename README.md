# 🏦 Loan Investment Supermarket

A production-grade **peer-to-peer lending marketplace** built with ASP.NET Core 10, Blazor WebAssembly, Clean Architecture, CQRS + MediatR, EF Core, and SQL Server.

This is not a tutorial CRUD application. It is a full-stack financial platform with real domain logic — amortization engines, installment state machines, background payment processing, role-based access control, and multi-role dashboards designed for regulatory compliance and operational transparency.

---

## 📸 Platform Overview

### Admin Command Centre

The admin dashboard provides real-time platform metrics — total funding volume, approval rates, application pipeline, and recent activity. Designed for operations teams who need instant visibility into platform health.

![Admin Dashboard](LoanSuperMarketScreens/admin/admin-dashboard.png)

---

### Borrower Management

Full borrower lifecycle management with verification status, credit tier assignment, and KYC document tracking. Supports search, filtering, and drill-down into individual profiles.

![Admin Borrowers](LoanSuperMarketScreens/admin/admin-borrowers.png)

---

### Lender Management

Monitor registered lenders, their verification status, available capital, and funded loan portfolios. Supports onboarding workflows and compliance checks.

![Admin Lenders](LoanSuperMarketScreens/admin/admin-lenders.png)

---

### Loan Applications Pipeline

End-to-end application lifecycle from submission through review, approval, and funding. CRM managers can approve, reject, or request additional documents with full audit trail.

![Loan Applications](LoanSuperMarketScreens/admin/admin-loan-applications.png)

![Application Details](LoanSuperMarketScreens/admin/admin-loan-applications-details.png)

---

### Loan Product Catalogue

Multi-lender product marketplace with configurable interest rates, term ranges, and amount limits. Products follow a Draft → Approval → Published lifecycle.

![Loan Products](LoanSuperMarketScreens/admin/admin-loan-products.png)

---

### Administration & Role Management

Granular role-based access control with 6 predefined roles, module-level permissions, and user account lifecycle management (Active, Hold, Blocked, Suspended).

![Administration Menu](LoanSuperMarketScreens/admin/admin-adminstration-menu.png)

---

## 💳 Borrower Journey

### Step 1: Loan Application Wizard

Guided multi-step application process: specify loan parameters, get matched with suitable products, select a product, upload KYC documents, and submit for review.

![Application Step 1](LoanSuperMarketScreens/borrowers/borrower-loan-application-step-1.png)

### Step 2: Product Matching & Selection

Intelligent product matching based on requested amount, term, and borrower credit tier. Displays effective interest rates and lender details for informed decision-making.

![Product Selection](LoanSuperMarketScreens/borrowers/borrower-loan-application-select-product.png)

### Step 3: Document Upload & KYC

Secure document upload for National ID, Proof of Income, Bank Statements, and Address Proof. Documents are stored securely and tracked through verification workflow.

![Document Upload](LoanSuperMarketScreens/borrowers/borrower-loan-application-documents.png)

### Step 4: Review & Submit

Final review of all application details before submission. Borrowers can verify their information and go back to make changes.

![Review & Submit](LoanSuperMarketScreens/borrowers/borrower-loan-application-review.png)

---

### Borrower Dashboard

Track all applications, active loans, payment history, and upcoming payments in one place. Includes loan calculator for pre-application planning.

![Borrower Dashboard](LoanSuperMarketScreens/borrowers/borrower-dashboard.png)

![All Applications](LoanSuperMarketScreens/borrowers/borrower-loan-applications-all.png)

---

### Active Loans & Payments

Real-time loan progress tracking with payment schedules, upcoming due dates, and payment history. Supports full and partial payments.

![Active Loans](LoanSuperMarketScreens/borrowers/borrower-active-loans.png)

![Upcoming Payments](LoanSuperMarketScreens/borrowers/borrower-upcoming-payments.png)

![Payments Dashboard](LoanSuperMarketScreens/borrowers/borrower-payments-dashboard.png)

---

### Loan Calculator

Interactive EMI calculator for borrowers to plan their applications before committing. Shows monthly payment, total interest, and total repayment.

![Loan Calculator](LoanSuperMarketScreens/borrowers/borrower-loan-calculator.png)

---

### Notification Preferences

Configurable notification settings for payment reminders, application updates, and platform communications.

![Notification Settings](LoanSuperMarketScreens/borrowers/notification-settings.png)

---

## 💼 Lender Journey

### Lender Dashboard

Quick-access command centre for lenders to manage their funding queue, portfolio, and earnings.

![Lender Dashboard](LoanSuperMarketScreens/lenders/lender-dashboard.png)

---

### Funding Queue

Review approved loan applications eligible for funding. Filter by product, amount range, and credit tier. Accept or decline with full decision audit trail.

![Funding Queue](LoanSuperMarketScreens/lenders/lender-funding-queue.png)

---

### Portfolio Overview

Complete portfolio view showing total funded amount, active loans, outstanding principal, expected monthly income, and default rate metrics.

![Portfolio Overview](LoanSuperMarketScreens/lenders/lender-portfolio.png)

---

### My Loans

Detailed view of all funded loans with performance badges (On Time, Late, Defaulted), repayment progress, and drill-down to individual schedules.

![My Loans](LoanSuperMarketScreens/lenders/lender-portfolio-my-loans.png)

---

### Earnings & Returns

Track interest earned, projected returns, late fees collected, and monthly income trends. Designed for investor reporting and tax documentation.

![Earnings](LoanSuperMarketScreens/lenders/lender-portfolio-my-earnings.png)

---

### Investor Analytics

ROI calculations, yield analysis, portfolio diversification metrics, and risk distribution across credit tiers.

![Analytics](LoanSuperMarketScreens/lenders/lender-portfolio-analytics.png)

---

### Lender Comparison

Benchmark your portfolio performance against other platform lenders. Compare returns, default rates, and funding volumes.

![Comparison](LoanSuperMarketScreens/lenders/lender-portfolio-comparison-to-other-lenders.png)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Blazor WebAssembly (Tailwind CSS + DaisyUI)                │
│  30+ reusable components, role-based navigation             │
└─────────────────────────┬───────────────────────────────────┘
                          │ HTTPS + JWT Bearer
┌─────────────────────────▼───────────────────────────────────┐
│  ASP.NET Core 10 API                                        │
│  Controllers + Policies + Middleware + SignalR               │
└─────────────────────────┬───────────────────────────────────┘
                          │ MediatR Pipeline
┌─────────────────────────▼───────────────────────────────────┐
│  Application Layer (CQRS)                                   │
│  Commands, Queries, Validators, Pipeline Behaviours         │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│  Domain Layer (DDD)                                         │
│  Entities, Value Objects, State Machines, Domain Services   │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│  Infrastructure                                             │
│  EF Core, Repositories, Identity, Background Services       │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│  SQL Server                                                 │
│  Migrations, Stored Procedures, Dapper Reporting            │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧩 Key Engineering Decisions

| Decision | Rationale |
|----------|-----------|
| **Rich Domain Entities** | State machines enforce business rules at the entity level — invalid transitions throw, not silently fail |
| **CQRS with MediatR** | Commands mutate state, queries read — never mixed. Pipeline behaviours handle cross-cutting concerns |
| **Resource-Filtered Queries** | `IResourceFilteredQuery` auto-scopes data by authenticated user role — lenders see their loans, borrowers see theirs |
| **Background Payment Processing** | `IHostedService` runs daily to detect late payments, apply fees, and trigger default detection |
| **Amortization Engine** | Standard EMI formula with rounding correction on final installment — principal always balances to zero |
| **JWT + Role Policies** | Fine-grained authorization with 6 roles, module-level permissions, and account status enforcement |
| **Clean Architecture** | Domain has zero dependencies on infrastructure — testable, portable, framework-agnostic |

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 10, C# 13 |
| Frontend | Blazor WebAssembly |
| Styling | Tailwind CSS + DaisyUI (Corporate theme) |
| Database | SQL Server + EF Core |
| Auth | ASP.NET Identity + JWT + Refresh Tokens |
| Messaging | MediatR (CQRS pipeline) |
| Validation | FluentValidation |
| Real-time | SignalR |
| Caching | IMemoryCache (cache-aside pattern) |
| Background | IHostedService |
| Testing | xUnit + Moq (30+ unit tests) |
| Architecture | Clean Architecture + CQRS + DDD |

---

## 📐 Project Structure

```
src/
├── LoanSuperMarket.Api/            # Controllers, middleware, SignalR hubs
├── LoanSuperMarket.Application/    # CQRS handlers, services, behaviours
├── LoanSuperMarket.Domain/         # Entities, value objects, enums, domain services
├── LoanSuperMarket.Infrastructure/ # EF Core, repositories, identity, background services
├── LoanSuperMarket.Shared/         # DTOs, requests, configuration contracts
└── LoanSuperMarket.Blazor/         # Pages, components, API clients, auth

tests/
├── LoanSuperMarket.Domain.Tests/
├── LoanSuperMarket.Application.Tests/
└── LoanSuperMarket.Api.Tests/

docs/                               # 11 comprehensive technical documents
```

---

## 🚀 Getting Started

```bash
# Clone
git clone https://github.com/your-username/LoanSuperMarketUsingBlazor.git

# Apply database migrations
cd LoanSuperMarketUsingBlazor
dotnet ef database update --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context ApplicationDbContext
dotnet ef database update --project src\LoanSuperMarket.Infrastructure --startup-project src\LoanSuperMarket.Api --context AuthIdentityDbContext

# Start API
cd src/LoanSuperMarket.Api
dotnet run

# Start Blazor (separate terminal)
cd src/LoanSuperMarket.Blazor
dotnet run
```

### Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@loansupermarket.com` | `Admin@123456!` |
| CRM Manager | `crm1@demo.com` | `Demo@12345!` |
| Customer Service | `staff1@demo.com` | `Demo@12345!` |
| Lender | `lender1@demo.com` | `Demo@12345!` |
| Borrower | `borrower1@demo.com` | `Demo@12345!` |

---

## ✅ Complete Feature Set

### Core Lending Lifecycle
- Multi-step loan application wizard with product matching
- CRM review workflow (approve, reject, request documents)
- Lender funding decision with capital management
- Amortization schedule generation (EMI formula)
- Payment processing (full, partial, bulk/early payoff)
- Late payment detection with grace period and fees
- Default detection (3+ consecutive missed payments)
- Loan restructuring for distressed loans

### Platform Operations
- JWT authentication with refresh tokens and 2FA
- 6 roles with granular module-level permissions
- SignalR real-time notifications
- Background hosted service for daily payment processing
- Audit trail for all significant actions
- Rate limiting (100 req/min per IP)
- Health checks and correlation ID middleware
- CSV export for reporting

### User Experience
- Role-based dashboards (Admin, Lender, Borrower)
- 30+ reusable Blazor components
- Dark mode toggle
- Loading skeletons and empty states
- Toast notifications and modal dialogs
- Responsive layout with professional styling

---

## 🧠 Engineering Philosophy

This project demonstrates how real financial platforms are engineered:

- **Domain logic lives in the domain** — not in controllers or stored procedures
- **State machines enforce correctness** — invalid transitions are impossible, not just discouraged
- **Every mutation is auditable** — who did what, when, and why
- **Data is scoped by role** — a lender cannot see another lender's portfolio
- **Background processing handles time-sensitive operations** — late fees don't depend on user actions
- **The architecture scales** — add a new feature without touching existing code

---

*Built to demonstrate senior full-stack engineering capability in regulated financial services.*
