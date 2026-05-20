# 🚀 Loan Investment Supermarket

An enterprise-grade loan marketplace platform built using **ASP.NET Core**, **Clean Architecture**, **CQRS**, **MediatR**, **Blazor WebAssembly**, **Tailwind CSS**, and **SQL Server**.

This repository is intentionally being developed as a realistic enterprise financial platform to demonstrate:

* scalable backend architecture,
* enterprise frontend engineering,
* reusable UI infrastructure,
* operational workflows,
* domain-driven business logic,
* and modern full-stack .NET engineering practices.

The goal is not just to build screens — but to model how real enterprise systems are designed, structured, scaled, and evolved over time.

---

# 🎯 Why This Project Exists

Most tutorial projects stop at:

* CRUD screens,
* simple APIs,
* demo dashboards.

This project focuses instead on:

* enterprise architecture,
* workflow-driven systems,
* operational UI patterns,
* scalable frontend composition,
* reusable infrastructure components,
* and realistic financial platform workflows.

The application is being built as if it were a real SaaS lending and investment platform used by:

* lenders,
* borrowers,
* operational teams,
* compliance teams,
* and marketplace administrators.

---

# 🖥️ Enterprise Frontend Architecture Focus

This repository is intentionally being engineered as a large-scale enterprise Blazor application rather than a simple CRUD demo.

A major focus of the platform is demonstrating how complex frontend systems are designed, structured, and maintained in real financial organisations.

The frontend architecture focuses heavily on:

* modular component-driven UI systems,
* reusable frontend infrastructure,
* workflow-driven operational interfaces,
* scalable state-aware UI composition,
* strongly typed API integration,
* enterprise-grade UX consistency,
* and maintainable frontend patterns suitable for long-term product evolution.

The application is designed to simulate the type of operational SaaS platform used internally by:

* lending operations teams,
* compliance departments,
* underwriting staff,
* customer support teams,
* and financial administrators.

The goal is to demonstrate not only frontend implementation skills — but also frontend architectural thinking.

---

## Frontend Engineering Objectives

This project intentionally focuses on enterprise frontend concerns such as:

### ✅ Reusable UI Infrastructure

* reusable modal systems
* toast notification infrastructure
* shared UI components
* centralized status rendering
* reusable operational tables
* dynamic workflow actions

### ✅ Component-Driven Architecture

* isolated reusable components
* EventCallback communication
* shared services
* typed API integration
* scalable layout composition
* maintainable UI separation

### ✅ Reactive Operational UX

* async workflow handling
* loading states
* operational dashboards
* workflow feedback
* reusable action patterns
* user interaction consistency

### ✅ Enterprise Blazor Patterns

* scoped UI state services
* layout-level infrastructure components
* centralized frontend orchestration
* reusable form patterns
* validation layering
* API-first frontend design

### ✅ Long-Term Maintainability

The repository is intentionally structured to model how enterprise frontend systems evolve over years rather than months.

This includes:

* clear component boundaries,
* scalable folder organisation,
* reusable infrastructure layers,
* workflow-focused UI design,
* and separation between UI, API, and business concerns.

The architecture aims to reflect how senior engineers structure large client-facing financial platforms.

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

Blazor WebAssembly was intentionally chosen to demonstrate enterprise SPA frontend architecture using C# and .NET across both frontend and backend systems.

The frontend is being designed similarly to large Angular or React enterprise applications, with:

* reusable component systems,
* shared UI infrastructure,
* typed API services,
* reactive workflows,
* centralized frontend patterns,
* and scalable operational UI composition.

The goal is to demonstrate how Blazor can be used beyond simple page rendering to build complex enterprise frontend systems.

The frontend communicates with the backend through strongly typed API clients similar to Angular or React enterprise applications.

---

# 🚀 Current Enterprise Features

## Backend Architecture

### ✅ Clean Architecture

The solution is separated into:

* API
* Application
* Domain
* Infrastructure
* Shared contracts
* Blazor frontend

This keeps responsibilities isolated and maintainable.

---

### ✅ CQRS + MediatR

Commands and queries are separated using CQRS patterns.

Examples implemented:

* Get Loan Products
* Get Loan Product By Id
* Create Loan Product
* Submit Loan Product For Approval
* Approve Loan Product
* Create Borrowers
* Create Lenders
* Create Loan Applications
* Dashboard Analytics Queries

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

Loan applications move through operational workflow stages:

```text
Submitted
→ UnderReview
→ Approved
→ Funded
→ Rejected
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

* Blazor `EditForm`
* `DataAnnotations`
* inline validation messages
* cross-field validation

#### Application Layer

* MediatR validation pipeline
* FluentValidation

#### Domain Layer

* business rule protection
* workflow transition rules

This mirrors real enterprise validation strategies.

---

# 🎨 Frontend Engineering

## ✅ Reusable Component Architecture

The frontend is intentionally structured around reusable components rather than page-specific markup.

Implemented reusable components:

* `PageHeader`
* `AppCard`
* `InfoTile`
* `StatusBadge`
* `AppDataTable`
* `MetricCard`
* `CreateLoanProductModal`
* `CreateBorrowerModal`
* `CreateLenderModal`
* `CreateLoanApplicationModal`
* `ToastContainer`

This creates a scalable UI foundation for future modules.

---

## ✅ Global Frontend Infrastructure

The frontend now includes reusable UI infrastructure patterns commonly used in enterprise frontend systems.

Implemented infrastructure includes:

* global toast notification system,
* reusable modal workflows,
* centralized workflow feedback,
* shared UI services,
* reactive component refresh patterns,
* and layout-level UI orchestration.

This demonstrates:

* component communication,
* shared frontend state,
* reusable frontend infrastructure,
* and scalable UX consistency across modules.

---

## ✅ Enterprise Operational UI

The project includes:

* dashboard-style layouts,
* operational tables,
* workflow actions,
* reusable status rendering,
* responsive layouts,
* async loading states,
* and workflow messaging.

The styling direction follows modern fintech and SaaS operational platforms.

---

## ✅ Typed API Clients

Blazor pages never call raw endpoints directly.

All API communication flows through typed client services such as:

```text
LoanProductsApiClient
BorrowersApiClient
LendersApiClient
LoanApplicationsApiClient
DashboardApiClient
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

## Application Review Workflow

```text
Submitted → UnderReview
```

## Application Approval Workflow

```text
UnderReview → Approved
```

## Funding Workflow

```text
Approved → Funded
```

Each workflow action is implemented through:

* explicit CQRS commands,
* MediatR handlers,
* domain methods,
* API workflow endpoints,
* and async Blazor UI actions.

This avoids dangerous generic update endpoints.

---

# 📊 Enterprise Dashboard & Analytics

The application now includes a reusable operational dashboard layer designed to simulate internal operational analytics systems.

Implemented analytics include:

* total borrowers
* total lenders
* total applications
* funded applications
* approval rates
* funding rates
* operational KPIs
* recent application activity
* recent borrower activity

The dashboard architecture demonstrates:

* CQRS-driven analytics queries
* repository aggregation patterns
* reusable metric UI components
* operational reporting UX
* and scalable dashboard composition.

---

# 🧩 Reusable Enterprise UI Infrastructure

A major focus of the project is reusable UI infrastructure.

## AppDataTable

Reusable operational table shell supporting:

* loading states
* empty states
* action areas
* reusable columns/rows
* future pagination/filtering
* operational workflows

This will later power:

* borrowers
* applications
* repayments
* approvals
* audit logs

---

## StatusBadge

Centralised workflow status rendering across the application.

One component controls:

* colours
* labels
* visual consistency
* workflow indicators

across all modules.

---

## Toast Notification Infrastructure

The application includes a reusable global toast notification system supporting:

* success notifications
* error notifications
* warning notifications
* information notifications
* animated lifecycle states
* automatic dismissal
* layout-level rendering

This demonstrates:

* reactive UI updates
* shared frontend state
* event-driven UI rendering
* component communication
* and reusable enterprise frontend infrastructure.

---

# 📈 What Makes This Different

This repository intentionally focuses on:

* enterprise maintainability,
* operational workflows,
* reusable frontend architecture,
* scalable backend patterns,
* realistic business processes,
* and enterprise UI engineering.

The goal is to demonstrate how senior-level systems are structured — not just how screens are rendered.

This project intentionally prioritises:

* architecture,
* maintainability,
* workflow modelling,
* frontend scalability,
* and reusable engineering patterns.

---

# 🔮 Planned Roadmap

The project is actively evolving.

## Upcoming Features

### Enterprise Modal Service

* centralized modal orchestration
* reusable confirmation dialogs
* dynamic modal rendering
* global dialog management

### Advanced Frontend State Management

* shared state containers
* workflow state management
* debounced search
* optimistic UI updates
* frontend caching

### Advanced Operational Data Grids

* pagination
* sorting
* filtering
* virtualization
* reusable query models

### Loan Product Publishing Workflow

```text
Approved → Published
```

### Archive Workflow

```text
Published → Archived
```

### Borrower Management

* onboarding
* KYC workflows
* customer profiles
* operational notes

### Loan Applications

* application submission
* approval queues
* underwriting workflows
* operational review flows

### Repayment Engine

* repayment schedules
* overdue logic
* payment tracking
* financial calculations

### Authentication & Authorization

* role-based security
* operational permissions
* admin controls

### Notifications System

* email workflows
* in-app alerts
* operational notifications
* workflow subscriptions

### Audit Logging

* workflow history
* operational tracking
* user actions
* compliance visibility

### Dashboard Analytics

* operational metrics
* lender analytics
* approval KPIs
* repayment summaries
* underwriting insights

### Azure Cloud Deployment

* App Services
* CI/CD pipelines
* Application Insights
* Azure Monitor
* production observability

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
✅ Component-driven UI systems
✅ Typed API integration
✅ Shared frontend infrastructure
✅ Reactive UI composition
✅ Long-term scalability

---

# 👨‍💻 Purpose of This Repository

This repository is intended as:

* an enterprise architecture showcase,
* a frontend engineering showcase,
* a real-world portfolio project,
* a learning platform,
* and an evolving example of modern ASP.NET Core + Blazor engineering.

The project is specifically being developed to demonstrate senior-level engineering capability across:

* backend architecture,
* enterprise frontend systems,
* reusable UI infrastructure,
* workflow-driven design,
* and scalable operational platforms.

---

# ⭐ Watch This Repository

The project is actively evolving into a full enterprise loan marketplace platform with:

* operational workflows,
* scalable frontend systems,
* approval pipelines,
* reusable UI infrastructure,
* financial domain modelling,
* cloud-ready architecture,
* and enterprise operational tooling.

Future updates will continue focusing on realistic enterprise engineering patterns rather than simple demo CRUD functionality.
