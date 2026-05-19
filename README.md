````md
# Loan Investment Supermarket

An enterprise-grade loan marketplace platform built using **ASP.NET Core**, **Clean Architecture**, **CQRS**, **MediatR**, **Blazor WebAssembly**, **Tailwind CSS**, and **SQL Server**.

This project demonstrates how to structure and build a modern full-stack financial platform using scalable backend architecture patterns and reusable frontend component design.

The platform is being developed as a realistic enterprise SaaS-style application for:
- loan marketplaces,
- lenders,
- borrowers,
- investment products,
- operational workflows,
- approvals,
- payments,
- dashboards,
- and analytics.

---

# 🚀 Technology Stack

## Backend
- ASP.NET Core
- C#
- Clean Architecture
- CQRS
- MediatR
- FluentValidation
- EF Core
- SQL Server
- Repository Pattern
- Global Exception Middleware
- Pipeline Behaviours
- REST APIs

## Frontend
- Blazor WebAssembly
- Tailwind CSS
- daisyUI
- Reusable Component Architecture
- Typed API Clients
- Enterprise Dashboard UI
- Responsive SaaS Layout

## Dev & Tooling
- Visual Studio 2022
- PowerShell Automation
- Git
- GitHub
- npm
- Tailwind Watch Pipeline

---

# 🏗️ Architecture Overview

The application follows a layered enterprise architecture:

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
````

---

# 🎯 Project Goals

This project is designed to demonstrate:

✅ Enterprise-grade architecture
✅ Real-world frontend/backend separation
✅ Scalable SaaS application structure
✅ Modern Blazor WebAssembly patterns
✅ Reusable UI component design
✅ Financial platform workflows
✅ Production-style CQRS implementation
✅ Clean separation of concerns
✅ Professional dashboard design

---

# ✨ Current Features

## Backend

* Clean Architecture setup
* CQRS command/query separation
* MediatR handlers
* FluentValidation pipeline
* Logging behaviour
* Performance behaviour
* Global exception middleware
* SQL Server persistence
* EF Core migrations
* Repository abstraction
* Domain-driven entity modelling

## Frontend

* Enterprise dashboard layout
* Responsive sidebar navigation
* Tailwind + daisyUI styling
* Reusable Blazor components
* Loan Products page
* API-connected product loading
* Create Loan Product modal
* Loading / error / empty states
* Typed HTTP API clients

---

# 🧩 Reusable UI Components

The frontend is structured around reusable components rather than page-specific markup.

Current component examples include:

* `PageHeader`
* `AppCard`
* `DashboardMetricCard`

Future reusable components planned:

* AppTable
* StatusBadge
* AppModal
* ConfirmDialog
* Toast Notifications
* Pagination Controls
* Charts & Analytics Panels
* Workflow Steppers

---

# 📸 UI Direction

The UI follows a modern fintech SaaS style inspired by enterprise dashboards used in:

* banking,
* insurance,
* investment,
* lending,
* and operational platforms.

Design goals:

* clean visual hierarchy,
* dark enterprise sidebar,
* responsive dashboards,
* reusable cards,
* operational KPIs,
* scalable layout system.

---

# 📂 Solution Structure

```text
src/
 ├── LoanSuperMarket.Api
 ├── LoanSuperMarket.Application
 ├── LoanSuperMarket.Domain
 ├── LoanSuperMarket.Infrastructure
 ├── LoanSuperMarket.Shared
 └── LoanSuperMarket.Blazor
```

---

# ⚙️ Running the Project

## Start Full Development Environment

```powershell
.\start-dev.ps1
```

This starts:

* Tailwind watcher
* ASP.NET Core API
* Blazor WebAssembly frontend

---

# 🌐 URLs

## API

```text
https://localhost:7117
```

## Swagger

```text
https://localhost:7117/swagger
```

## Blazor Frontend

```text
http://localhost:5036
```

---

# 🧪 Development Workflow

## Tailwind Watcher

```powershell
npx tailwindcss -i ./wwwroot/css/tailwind-input.css -o ./wwwroot/css/app.css --watch
```

---

# 📈 Planned Features

## Marketplace Features

* Borrower onboarding
* Lender management
* Investment products
* Loan applications
* Approval workflows
* Repayment schedules
* Payment processing
* Notifications
* Document management

## Technical Features

* Authentication & Authorization
* Role-based access control
* SignalR real-time notifications
* Audit logging
* Dashboard analytics
* Background processing
* Azure deployment
* CI/CD pipelines
* Docker support
* Testing strategy

---

# 💡 Why Blazor WebAssembly?

Blazor WebAssembly was intentionally chosen to demonstrate:

* SPA-style frontend architecture
* frontend/backend separation
* scalable API-first design
* reusable component systems
* enterprise dashboard UI patterns

The frontend communicates with the backend through typed HTTP API clients similar to Angular or React enterprise applications.

---

# 🧠 Engineering Principles

This project focuses heavily on:

* Separation of concerns
* Thin controllers
* Rich application layer
* Reusable UI components
* Centralised validation
* Centralised exception handling
* Operational observability
* Scalable frontend architecture
* Enterprise maintainability

---

# 👨‍💻 Author

Built by an enterprise .NET developer focused on:

* scalable backend systems,
* financial platforms,
* cloud-native applications,
* frontend architecture,
* and operationally reliable software engineering.

---

# ⭐ Purpose of This Repository

This repository is intended as:

* an enterprise architecture showcase,
* a learning platform,
* a portfolio project,
* and a real-world example of modern ASP.NET Core + Blazor engineering.

---

```
```
