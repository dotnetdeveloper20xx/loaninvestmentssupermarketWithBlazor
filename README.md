# 🚀 Loan Investment Supermarket

An enterprise-grade loan marketplace platform built using **ASP.NET Core**, **Clean Architecture**, **CQRS**, **MediatR**, **Blazor WebAssembly**, **Tailwind CSS**, and **SQL Server**.

This repository is intentionally being developed as a realistic enterprise financial platform to demonstrate:
- scalable backend architecture,
- reusable frontend engineering,
- operational workflows,
- domain-driven business logic,
- and modern full-stack .NET engineering practices.

The goal is not just to build screens — but to model how real enterprise systems are designed, structured, and evolved over time.

---

# 🎯 Why This Project Exists

Most tutorial projects stop at:
- CRUD screens,
- simple APIs,
- demo dashboards.

This project focuses instead on:
- enterprise architecture,
- workflow-driven systems,
- operational UI patterns,
- scalable frontend composition,
- reusable infrastructure components,
- and realistic financial platform workflows.

The application is being built as if it were a real SaaS lending and investment platform used by:
- lenders,
- borrowers,
- operational teams,
- compliance teams,
- and marketplace administrators.

---

# 🏗️ Architecture & Engineering Approach

The solution follows a layered enterprise architecture:

```text
Blazor WebAssembly
        ↓
Typed API Clients
        ↓
ASP.NET Core API
        ↓
CQRS + MediatR
        ↓
Application Layer
        ↓
Domain Layer
        ↓
Infrastructure Layer
        ↓
SQL Server
```

---

# ⚡ Why Blazor WebAssembly?

Blazor WebAssembly was intentionally chosen to demonstrate:
- SPA-style frontend architecture,
- API-first design,
- reusable component systems,
- enterprise frontend patterns,
- and frontend/backend separation.

The frontend communicates with the backend through strongly typed API clients similar to Angular or React enterprise applications.

---

# 🚀 Current Enterprise Features

## Backend Architecture

### ✅ Clean Architecture

The solution is separated into:
- API
- Application
- Domain
- Infrastructure
- Shared contracts
- Blazor frontend

This keeps responsibilities isolated and maintainable.

---

### ✅ CQRS + MediatR

Commands and queries are separated using CQRS patterns.

Examples implemented:
- Get Loan Products
- Get Loan Product By Id
- Create Loan Product
- Submit Loan Product For Approval
- Approve Loan Product

This enables scalable workflow-driven architecture rather than generic CRUD endpoints.

---

### ✅ Domain-Driven Workflow Logic

Loan products move through explicit workflow states:

```text
Draft
→ PendingApproval
→ Approved
→ Published
→ Archived
```

Workflow transitions are protected inside the domain model through methods such as:

```csharp
SubmitForApproval()
Approve()
Publish()
Archive()
```

The UI never directly changes statuses.

This models how real enterprise workflow systems operate.

---

### ✅ Validation Layers

The application demonstrates validation at multiple architectural levels.

#### Frontend
- Blazor `EditForm`
- `DataAnnotations`
- inline validation messages
- cross-field validation

#### Application Layer
- MediatR validation pipeline
- FluentValidation

#### Domain Layer
- business rule protection
- workflow transition rules

This mirrors real enterprise validation strategies.

---

# 🎨 Frontend Engineering

## ✅ Reusable Component Architecture

The frontend is intentionally structured around reusable components rather than page-specific markup.

Implemented reusable components:

- `PageHeader`
- `AppCard`
- `InfoTile`
- `StatusBadge`
- `AppDataTable`
- `CreateLoanProductModal`

This creates a scalable UI foundation for future modules.

---

## ✅ Enterprise Operational UI

The project includes:
- dashboard-style layouts,
- operational tables,
- workflow actions,
- reusable status rendering,
- responsive layouts,
- async loading states,
- and workflow messaging.

The styling direction follows modern fintech and SaaS operational platforms.

---

## ✅ Typed API Clients

Blazor pages never call raw endpoints directly.

All API communication flows through typed client services such as:

```text
LoanProductsApiClient
```

This keeps UI concerns clean and maintainable.

---

# 🔄 Real Workflow Actions

The platform already demonstrates operational business workflows.

Implemented workflow actions include:

## Submit for Approval

```text
Draft → PendingApproval
```

## Approve Product

```text
PendingApproval → Approved
```

Each workflow action is implemented through:
- explicit CQRS commands,
- MediatR handlers,
- domain methods,
- API workflow endpoints,
- and async Blazor UI actions.

This avoids dangerous generic update endpoints.

---

# 🧩 Reusable Enterprise UI Infrastructure

A major focus of the project is reusable UI infrastructure.

## AppDataTable

Reusable operational table shell supporting:
- loading states
- empty states
- action areas
- reusable columns/rows
- future pagination/filtering

This will later power:
- borrowers
- applications
- repayments
- approvals
- audit logs

---

## StatusBadge

Centralised workflow status rendering across the application.

One component controls:
- colours
- labels
- visual consistency

across all modules.

---

## CreateLoanProductModal

Demonstrates:
- reusable modal architecture,
- Blazor `EditForm`,
- validation,
- API workflows,
- component composition,
- and EventCallback communication.

---

# 📈 What Makes This Different

This repository intentionally focuses on:
- enterprise maintainability,
- operational workflows,
- reusable frontend architecture,
- scalable backend patterns,
- and realistic business processes.

The goal is to demonstrate how senior-level systems are structured — not just how screens are rendered.

---

# 🔮 Planned Roadmap

The project is actively evolving.

## Upcoming Features

### Loan Product Publishing Workflow

```text
Approved → Published
```

### Archive Workflow

```text
Published → Archived
```

### Borrower Management
- onboarding
- KYC workflows
- customer profiles

### Loan Applications
- application submission
- approval queues
- underwriting workflows

### Repayment Engine
- repayment schedules
- overdue logic
- payment tracking

### Authentication & Authorization
- role-based security
- operational permissions
- admin controls

### Notifications System
- email workflows
- in-app alerts
- operational notifications

### Audit Logging
- workflow history
- operational tracking
- user actions

### Dashboard Analytics
- operational metrics
- lender analytics
- approval KPIs
- repayment summaries

### Azure Cloud Deployment
- App Services
- CI/CD pipelines
- Application Insights
- production observability

---

# 🧠 Engineering Principles

This project heavily focuses on:

✅ Separation of concerns  
✅ Thin controllers  
✅ Rich domain behaviour  
✅ Explicit workflows  
✅ Reusable components  
✅ Centralised validation  
✅ Operational UI patterns  
✅ Async workflow handling  
✅ Enterprise maintainability  
✅ Scalable frontend architecture  

---

# 👨‍💻 Purpose of This Repository

This repository is intended as:

- an enterprise architecture showcase,
- a real-world portfolio project,
- a learning platform,
- and an evolving example of modern ASP.NET Core + Blazor engineering.

---

# ⭐ Watch This Repository

The project is actively evolving into a full enterprise loan marketplace platform with:
- operational workflows,
- scalable frontend systems,
- approval pipelines,
- financial domain modelling,
- and cloud-ready architecture.

Future updates will continue focusing on realistic enterprise engineering patterns rather than simple demo CRUD functionality.

---
