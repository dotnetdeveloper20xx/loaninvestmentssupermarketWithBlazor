# 🏦 Loan Investment Supermarket

A production-grade peer-to-peer lending marketplace built with **ASP.NET Core 10**, **Blazor WebAssembly**, **Clean Architecture**, **CQRS + MediatR**, **EF Core**, and **SQL Server**.

This isn't a tutorial CRUD app. It's a full-stack financial platform with real domain logic — amortization engines, state machines, background payment processing, and role-scoped dashboards.

---

## 🔥 What's New — Lender Funding & Repayment Engine

The platform now includes a **complete lending lifecycle** from funding decision through repayment tracking:

### Funding Workflow
- **Funding Queue** — Lenders see approved applications for their products, filtered by amount/product
- **Funding Decision** — Accept (deducts capital, generates amortization schedule) or Decline with reason
- **Capital Management** — Real-time available funds tracking with insufficient-funds protection

### Amortization Engine
- **EMI Formula** — Standard `P × r × (1+r)^n / ((1+r)^n - 1)` calculation
- **Credit Tier Rate Adjustment** — Tier A (base), Tier B (+2%), Tier C (+4%)
- **Rounding Invariant** — Final installment absorbs rounding difference so principal always balances

### Payment Processing
- **Sequential Payment Enforcement** — Only the next pending installment can be paid
- **Full & Partial Payments** — State machine transitions: Pending → PartiallyPaid → Paid
- **Late Detection** — Background service marks installments Late after grace period, applies late fees
- **Missed & Default Detection** — Late → Missed transitions, 3+ consecutive = Defaulted
- **Notification Hooks** — Reminders, late notices, default alerts (stub implementation, ready for email/SMS)

### Dashboards
- **Lender Portfolio** — Total funded, active loans, outstanding principal, expected monthly income, default rate
- **Lender Loans** — Performance badges (OnTime/Late/Defaulted), filtering, drill-down to schedule
- **Lender Earnings** — Interest received, projected returns, late fees collected
- **Borrower Active Loans** — Progress bars, due-soon warnings, late/missed highlights
- **Borrower Payment History** — Full payment trail with principal/interest breakdown
- **Upcoming Payments Calendar** — Next 3 months across all active loans

---

## 🏗️ Architecture

```
Blazor WebAssembly (Tailwind CSS)
        ↓
Typed API Clients
        ↓
ASP.NET Core API (Controllers + Policies)
        ↓
MediatR Pipeline (Validation → Limits → Auth → Handler)
        ↓
Application Layer (CQRS Commands/Queries)
        ↓
Domain Layer (Entities + Value Objects + Domain Services)
        ↓
Infrastructure (EF Core + Background Services)
        ↓
SQL Server
```

### Key Patterns
- **Domain-Driven Design** — Rich entities with state machines, not anemic models
- **CQRS** — Commands mutate, queries read, never mixed
- **Pipeline Behaviours** — Cross-cutting validation, logging, limit enforcement, resource authorization
- **Resource-Filtered Queries** — `IResourceFilteredQuery` auto-scopes data by user role
- **Background Processing** — `IHostedService` for daily late payment detection

---

## 🧩 Domain Model Highlights

### Installment State Machine
```
Pending → Paid (full payment)
Pending → PartiallyPaid → Paid (incremental payments)
Pending/PartiallyPaid → Late (grace period expired)
Late → Missed (next due date arrived)
```

### Loan Performance
```
OnTime — all payments current
Late — one or more installments overdue
Defaulted — 3+ consecutive Late/Missed
```

### Lender Capital
```csharp
lender.DeductFunds(amount);  // throws if insufficient
```

---

## ✅ Completed Features

| Layer | Feature |
|-------|---------|
| **Domain** | Loan Products, Borrowers, Lenders, Applications, Installments, Repayment Schedules |
| **Domain** | Value Objects (Money, InterestRate), Enums (CreditTier, InstallmentStatus, LoanPerformance) |
| **Domain** | PaymentProcessor domain service with sequential enforcement |
| **Application** | Full CQRS for funding, payments, dashboards (15+ command/query handlers) |
| **Application** | AmortizationService with EMI formula and rounding correction |
| **Application** | LatePaymentService — overdue detection, missed transitions, default detection, reminders |
| **Application** | Pipeline behaviours — validation, logging, performance, limit enforcement, resource auth |
| **Infrastructure** | EF Core with full entity configurations, indexes, precision settings |
| **Infrastructure** | Background hosted service for daily payment processing |
| **Infrastructure** | Stub notification service (logs only — ready for real email/SMS) |
| **API** | FundingController, PaymentsController, DashboardController (lender + borrower endpoints) |
| **API** | LoanApplications, LoanProducts, Lenders, Borrowers, Auth, Roles, Sessions, Audit |
| **Blazor** | Funding Queue page with filters |
| **Blazor** | Funding Decision modal (accept/decline with confirmation) |
| **Blazor** | Repayment Schedule page with installment table and inline payment |
| **Blazor** | Payment Form component with validation |
| **Blazor** | Lender Dashboard (Portfolio / Loans / Earnings tabs) |
| **Blazor** | Borrower Dashboard (Applications / Active Loans / Payment History / Upcoming tabs) |
| **Blazor** | Enterprise DataGrid, Modal, Drawer, Toast, Form infrastructure |
| **Auth** | JWT with refresh tokens, role-based policies, 2FA support |
| **Auth** | Account status enforcement, session management, permission resolution |

---

## 🎯 What's Next

| Priority | Feature | Description |
|----------|---------|-------------|
| 🔴 | **EF Migration** | Generate and apply migration for RepaymentSchedules + Installments tables |
| 🔴 | **Real Lender Resolution** | Wire FundingController to resolve lender from authenticated user claims |
| 🟡 | **SignalR Real-Time Updates** | Live funding queue refresh, payment confirmations |
| 🟡 | **Email Notifications** | Replace stub with SendGrid/SMTP for reminders and alerts |
| 🟡 | **Repayment Reports** | PDF schedule export, monthly lender statements |
| 🟢 | **Early Repayment** | Allow borrowers to pay ahead with interest recalculation |
| 🟢 | **Loan Restructuring** | Extend term, adjust rate for distressed loans |
| 🟢 | **Investor Analytics** | ROI calculations, portfolio diversification metrics |
| 🟢 | **Azure Deployment** | App Services, CI/CD, Application Insights, Azure Monitor |
| 🟢 | **Property-Based Testing** | FsCheck/Hedgehog tests for amortization invariants |

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 10, C# 13 |
| Frontend | Blazor WebAssembly |
| Styling | Tailwind CSS |
| Database | SQL Server + EF Core |
| Auth | ASP.NET Identity + JWT |
| Messaging | MediatR |
| Validation | FluentValidation |
| Architecture | Clean Architecture + CQRS + DDD |

---

## 🚀 Getting Started

```bash
# Clone
git clone https://github.com/your-username/loaninvestmentssupermarketWithBlazor.git

# Backend
cd src/LoanSuperMarket.Api
dotnet run

# Frontend (separate terminal)
cd src/LoanSuperMarket.Blazor
dotnet run
```

Default admin: `admin@loansupermarket.com` / `Admin@123456!`

---

## 📐 Project Structure

```
src/
├── LoanSuperMarket.Api/            # Controllers, middleware, Program.cs
├── LoanSuperMarket.Application/    # CQRS handlers, services, interfaces
├── LoanSuperMarket.Domain/         # Entities, value objects, enums, domain services
├── LoanSuperMarket.Infrastructure/ # EF Core, repositories, identity, background services
├── LoanSuperMarket.Shared/         # DTOs, requests, configuration contracts
└── LoanSuperMarket.Blazor/         # Pages, components, API clients, auth
```

---

## 🧠 Engineering Philosophy

This project prioritises:

- **Real domain logic** over CRUD wrappers
- **State machines** over status string updates
- **Sequential correctness** over optimistic shortcuts
- **Layered validation** (domain → application → API → UI)
- **Background processing** for time-sensitive operations
- **Role-scoped data** — lenders see their loans, borrowers see theirs
- **Composable UI** — reusable components, not copy-paste markup

---

*Built to demonstrate how real financial platforms are engineered — not just how screens are rendered.*
