# Business Overview — Loan Investment Supermarket

## What Is This Application?

Let me explain this in the simplest way possible.

Think of it like this — imagine you have two groups of people:

1. **People who have money** and want to earn returns on it (Lenders/Investors)
2. **People who need money** for things like home renovations, business expansion, or emergencies (Borrowers)

This application is the **marketplace** that connects them. It's like an eBay for loans — but instead of buying products, lenders are buying loan positions, and instead of selling items, borrowers are selling their promise to repay with interest.

The reason this exists is because traditional banks are slow, expensive, and don't serve everyone. Peer-to-peer lending platforms cut out the middleman and let money flow directly between people who have it and people who need it.

---

## The Industry — Peer-to-Peer Lending (P2P)

From the business point of view, this is a **fintech platform** operating in the peer-to-peer lending space. Real-world examples include:

- **Funding Circle** (UK) — business loans
- **Zopa** (UK) — personal loans
- **LendingClub** (US) — consumer credit

What usually happens in real life is:
- A borrower applies for a loan
- The platform assesses their creditworthiness
- The loan is listed for investors/lenders to fund
- Once funded, the borrower receives the money
- Monthly repayments flow back to the lender with interest

Our platform does exactly this — end to end.

---

## Why Does This Application Exist?

The business problem it solves:

1. **For Borrowers** — Access to credit that might not be available from traditional banks, with competitive rates based on their credit profile
2. **For Lenders** — Higher returns than savings accounts or bonds, with the ability to diversify across multiple loans
3. **For the Platform** — Revenue from fees on funded loans (origination fees, servicing fees)

---

## What Happens If The System Fails?

This becomes important because financial systems have real consequences:

- **If the funding queue breaks** — Approved loans sit unfunded, borrowers wait, lenders can't deploy capital
- **If the payment engine breaks** — Installments aren't tracked, late fees aren't applied, lenders don't get paid
- **If the amortization calculator is wrong** — Interest calculations are incorrect, regulatory risk, financial loss
- **If authentication breaks** — Unauthorized access to financial data, compliance violation
- **If the late detection service stops** — Defaults go unnoticed, lenders lose money without warning

This is why the system is built with enterprise patterns — it's not a toy, it handles real money flows.

---

## Who Depends On This System?

| Stakeholder | What They Care About |
|-------------|---------------------|
| Borrowers | Getting funded quickly, fair rates, clear repayment schedule |
| Lenders | Returns on investment, portfolio visibility, risk management |
| Operations Team | Processing applications, managing defaults, platform health |
| Compliance | Audit trails, proper documentation, regulatory reporting |
| Management/CEO | Platform growth, default rates, total funded volume |
| Support Team | Resolving user issues, understanding workflows |

---

## Real-Life Operational Scenarios

Now imagine a typical day on this platform:

**Morning:**
- A borrower named Emma logs in and applies for a £25,000 home renovation loan
- She goes through the wizard, selects a product, uploads her documents
- The application enters the review queue

**Midday:**
- A CRM manager reviews Emma's application
- They check her documents, verify her income
- They approve the application with a note: "Strong income, low debt ratio"

**Afternoon:**
- James, a lender at Apex Capital, opens his Funding Queue
- He sees Emma's approved application: £25,000, Tier B credit, 14% effective rate
- He clicks "Accept Funding" — his capital is deducted, a 36-month repayment schedule is generated
- Emma gets notified: "Your loan has been funded!"

**Monthly:**
- Emma makes her £854.67 monthly payment
- The system records it against installment #1
- James sees the payment reflected in his earnings dashboard

**If Emma misses a payment:**
- After 5 days (grace period), the system marks the installment as Late
- A 2% late fee is applied
- Emma gets a late payment notice
- If she misses 3 in a row, the loan enters Default
- It appears in the Admin Collections queue

This is the full lifecycle that our application manages.

---

## The Technology Choice — Why These Tools?

| Technology | Why It Was Chosen |
|-----------|------------------|
| ASP.NET Core 10 | Enterprise-grade, high performance, Microsoft ecosystem |
| Blazor WebAssembly | Full C# stack (no JavaScript), SPA experience, shared DTOs |
| Clean Architecture | Maintainability, testability, separation of concerns |
| CQRS + MediatR | Scalable command/query separation, pipeline behaviours |
| EF Core | ORM for rapid development, migrations, LINQ queries |
| Dapper | Raw SQL performance for reporting queries |
| SQL Server | Enterprise database, stored procedures, reliability |
| SignalR | Real-time notifications without polling |
| JWT + Identity | Industry-standard authentication and authorization |
| Tailwind CSS | Rapid UI development, consistent design system |

The reason the team chose this stack is because it demonstrates how a single team can build a complete financial platform using one language (C#) across the entire stack — from database to browser.
