# Documentation Index — The Project Bible

## Loan Investment Supermarket — Complete Technical Documentation

This is not a summary. This is the BIBLE of the project. Every class, every
method, every pattern, every decision is explained in detail. A junior
developer reading this cover-to-cover will understand the system deeply
enough to maintain, improve, and explain it confidently.

---

## How to Use This Documentation

### If You're Brand New (Day 1)

Start here and read in this exact order:

1. **Read [01 - Business Overview](01-business-overview.md) first** — Understand WHAT we're building and WHY. Don't touch code until you understand the business. This takes 15 minutes and saves you hours of confusion later.

2. **Read [02 - User Roles & Journeys](02-user-roles-and-journeys.md) next** — Understand WHO uses the system and WHAT they do daily. Picture real humans clicking buttons. This gives you context for every feature.

3. **Read [03 - Architecture Overview](03-architecture-overview.md)** — Understand HOW the code is organized. The layer diagram is your map. Come back to this whenever you're lost about where something lives.

After these three, you understand the business, the users, and the structure. You're ready to read code.

---

### If You're Starting to Read Code (Week 1)

4. **Read [04 - Domain Layer Bible](04-domain-layer-deep-dive.md)** — This is the longest document. Read it with the actual code open side-by-side. Every method is explained line-by-line. Start with `BaseEntity`, then `Money`, then `Lender`, then `Installment`.

5. **Read [05 - Application Layer Bible](05-application-layer-deep-dive.md)** — Understand how handlers orchestrate domain objects. The `FundLoanCommandHandler` walkthrough is the most important section — it shows how a complete business operation flows.

6. **Read [11 - Design Patterns Bible](11-design-patterns-explained.md)** — Now that you've seen the code, understand WHY it's structured that way. This document explains the reasoning behind every architectural decision.

---

### If You're Debugging an Issue (Any Time)

Jump directly to **[10 - Troubleshooting & Support](10-troubleshooting-and-support.md)**. It has:
- Common issues with exact fixes
- SQL queries for investigating data
- Step-by-step debugging process
- Performance monitoring tips

---

### If You're Adding a New Feature

1. Read [03 - Architecture](03-architecture-overview.md) to remember the layer structure
2. Read [05 - Application Layer](05-application-layer-deep-dive.md) to see the handler pattern
3. Read [08 - API Endpoints](08-api-endpoints-catalog.md) to see how endpoints are structured
4. Read [07 - Frontend](07-frontend-blazor-deep-dive.md) to see the page/component pattern
5. Follow the existing feature folder structure exactly

---

### If You're Preparing for a Technical Discussion

Read these in order:
1. [03 - Architecture](03-architecture-overview.md) — Layer structure and CQRS
2. [11 - Design Patterns](11-design-patterns-explained.md) — 17 patterns with justification
3. [04 - Domain Layer](04-domain-layer-deep-dive.md) — State machines and DDD
4. [09 - Testing](09-testing-strategy.md) — What we test and why

---

### If You're Onboarding a New Team Member

Give them:
- Day 1: Documents 01, 02, 03
- Day 2-3: Documents 04, 05
- Day 4-5: Documents 06, 07, 08
- Week 2: Documents 09, 10, 11 as reference

---

### Quick Reference (Bookmark These)

| I need to... | Go to... |
|-------------|----------|
| Understand a domain entity | [04 - Domain Layer](04-domain-layer-deep-dive.md) |
| Find an API endpoint | [08 - API Endpoints](08-api-endpoints-catalog.md) |
| Debug a production issue | [10 - Troubleshooting](10-troubleshooting-and-support.md) |
| Understand why code is structured this way | [11 - Design Patterns](11-design-patterns-explained.md) |
| Add a new Blazor page | [07 - Frontend](07-frontend-blazor-deep-dive.md) |
| Write a new test | [09 - Testing](09-testing-strategy.md) |
| Query the database | [06 - Database](06-database-and-sql.md) |

---

## Documents

| # | Document | What You'll Learn |
|---|----------|-------------------|
| 01 | [Business Overview](01-business-overview.md) | The industry, the problem, stakeholders, what happens if it breaks |
| 02 | [User Roles & Journeys](02-user-roles-and-journeys.md) | Every user type, their daily workflows, screens, permissions |
| 03 | [Architecture Overview](03-architecture-overview.md) | Clean Architecture layers, MediatR pipeline, database strategy |
| 04 | [Domain Layer Bible](04-domain-layer-deep-dive.md) | EVERY entity method-by-method, state machines, value objects, domain services |
| 05 | [Application Layer Bible](05-application-layer-deep-dive.md) | CQRS handlers, AmortizationService, LatePaymentService, pipeline behaviours |
| 06 | [Database & SQL Bible](06-database-and-sql.md) | Tables, relationships, stored procedures, Dapper, EF Core configs |
| 07 | [Frontend (Blazor) Bible](07-frontend-blazor-deep-dive.md) | 30+ components, pages, API clients, auth flow, SignalR, dark mode |
| 08 | [API Endpoints Bible](08-api-endpoints-catalog.md) | Every endpoint with auth, body, response, side effects |
| 09 | [Testing Strategy](09-testing-strategy.md) | Every test explained, what it proves, how to run |
| 10 | [Troubleshooting & Support](10-troubleshooting-and-support.md) | Common issues, debugging steps, SQL investigation queries |
| 11 | [Design Patterns Bible](11-design-patterns-explained.md) | 17 patterns with where/why/evidence in code |

---

## How to Use This Documentation

**If you're new to the project:** Start with documents 01-03. They give you the big picture.

**If you're debugging an issue:** Jump to document 10 (Troubleshooting).

**If you're adding a new feature:** Read documents 03-05 to understand the patterns, then follow the existing feature folder structure.

**If you're preparing for a technical discussion:** Documents 11 and 03 cover the architectural decisions and patterns.

**If you're onboarding a new team member:** Give them documents 01-03 on day 1, documents 04-07 on week 1, and the rest as reference material.
