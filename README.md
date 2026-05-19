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
