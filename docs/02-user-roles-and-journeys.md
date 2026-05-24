# User Roles & Journeys

## Overview of Roles

This platform has distinct user roles, each with different permissions, screens, and business goals. Think of it like a building with different floors — everyone enters through the same door (login), but they go to different floors based on who they are.

---

## Role 1: Borrower

### Who Are They?
Individuals or small business owners who need money. They might need a personal loan for home improvements, a car, or business expansion capital.

### What They Care About
- Getting approved quickly
- Getting the best interest rate for their credit profile
- Understanding their repayment obligations
- Making payments easily
- Knowing their loan status at all times

### Their Daily Experience

**First Visit:**
1. Register an account → lands on the home page
2. Click "Apply for Loan" → enters the loan wizard
3. Fill in amount, term, purpose
4. Get matched with suitable loan products
5. Select a product and submit the application
6. Upload required documents (ID, proof of income)

**After Submission:**
- Wait for CRM review (usually 24-48 hours)
- Get notified when status changes
- If documents are requested, upload them
- If approved, wait for a lender to fund

**After Funding:**
- View their repayment schedule (exact dates, amounts)
- See the payment timeline (visual blocks showing progress)
- Make monthly payments via the "Pay" button
- Or pay off the entire loan early with "Pay All Remaining"
- Check upcoming payments calendar
- Use the loan calculator for "what if" scenarios

### Screens They Use
- `/borrower-dashboard` — Applications tab, Active Loans tab, Payment History, Upcoming, Calculator
- `/repayment-schedule/{id}` — Detailed installment table with payment actions
- `/wizard` — Loan application wizard
- `/account/profile` — Personal details
- `/account/notifications` — Notification preferences
- `/help` — FAQ

### Permissions
- Can only see their own applications and loans
- Cannot see other borrowers' data
- Cannot access lender or admin screens
- Can make payments on their own schedules

---

## Role 2: Lender

### Who Are They?
Companies or individuals with capital to deploy. They want to earn returns by funding loans. Think of them as investors — they're putting money to work.

### What They Care About
- Finding good loans to fund (low risk, good returns)
- Portfolio diversification (not all eggs in one basket)
- Tracking their earnings (interest income, late fees)
- Managing their available capital
- Understanding default risk
- Restructuring distressed loans

### Their Daily Experience

**Morning Routine:**
1. Login → see the home page with quick-action cards
2. Check "My Portfolio" → see total funded, active loans, default rate
3. Check "Funding Queue" → see new approved applications waiting for funding
4. Review an application → see borrower details, credit tier, rate, purpose
5. Decide: Accept (fund it) or Decline (pass)

**When They Fund a Loan:**
- Click "Accept Funding"
- System deducts the amount from their available capital
- Amortization schedule is generated automatically
- They start receiving monthly payments

**Portfolio Management:**
- Check the "Loans" tab → see all funded loans with performance badges
- Check the "Earnings" tab → total interest received, projected returns
- Check the "Analytics" tab → ROI per loan, annualized yield, diversification score
- Check the "Comparison" tab → which products perform best
- Top up capital when funds run low (click the Available Funds card)

**When a Loan Goes Bad:**
- See the performance badge change from "OnTime" to "Late"
- If it hits "Defaulted", consider restructuring
- Click "Restructure Loan" → adjust rate and term to help the borrower recover

### Screens They Use
- `/funding-queue` — Browse and fund approved applications
- `/lender-dashboard` — Portfolio, Loans, Earnings, Analytics, Comparison tabs
- `/repayment-schedule/{id}` — View any funded loan's installment details
- `/account/profile` — Personal details
- `/account/notifications` — Notification preferences

### Permissions
- Can only see loans they've funded
- Can only fund applications for their own products
- Cannot see other lenders' portfolios
- Can restructure their own distressed loans

---

## Role 3: CRM Manager

### Who Are They?
Operations staff who review and process loan applications. They're the gatekeepers — they decide if an application is good enough to be approved.

### What They Care About
- Processing the review queue efficiently
- Making fair, consistent decisions
- Requesting additional documents when needed
- Maintaining audit trails for compliance

### Their Daily Experience

1. Login → go to "Review Queue"
2. See pending applications sorted by submission date
3. Click into an application → see borrower details, documents, credit info
4. Decision: Approve (with reason), Reject (with reason), or Request Documents
5. Move to the next application

### Screens They Use
- `/review-queue` — List of applications awaiting review
- `/loan-applications` — All applications with status filters
- `/borrowers` — Borrower profiles and verification status

### Permissions
- Can view all applications
- Can approve/reject applications
- Can request documents
- Cannot fund loans
- Cannot access admin panels

---

## Role 4: Admin

### Who Are They?
Platform administrators with full access. They monitor the entire system, manage users, handle escalations, and ensure the platform operates correctly.

### What They Care About
- Platform health (default rates, total funded, active users)
- User management (approving new accounts, managing roles)
- Collections (handling defaulted loans)
- System configuration

### Their Daily Experience

1. Login → see the home page with all quick-action cards
2. Check "All Loans" → platform-wide view of every funded loan
3. Check "Collections" → defaulted loans needing action
4. Check "User Management" → approve pending accounts, assign roles
5. Monitor health endpoint → `/health`

### Screens They Use
- `/admin/loans` — Platform-wide loan overview with filters
- `/admin/collections` — Defaulted loans and recovery workflow
- `/admin/users` — User management
- `/admin/roles` — Role and permission management
- All other screens (full access)

### Permissions
- Full access to everything
- Can see all users' data
- Can override statuses
- Can manage roles and permissions

---

## Role 5: Customer Service

### Who Are They?
Support staff who handle user inquiries, disputes, and operational issues.

### Screens They Use
- `/disputes` — Customer disputes
- `/messages` — Communication with users

---

## The Authentication Flow

What happens when ANY user logs in:

1. User enters email + password on `/auth/login`
2. API validates credentials against ASP.NET Identity
3. If valid, returns JWT access token (15 min) + refresh token (7 days)
4. Blazor stores both in localStorage
5. Every API call includes the access token in the `Authorization: Bearer` header
6. When the access token expires, the refresh token automatically gets a new one
7. The sidebar navigation shows/hides links based on the user's roles
8. Each API endpoint checks the user's role before processing

This is why you see `[Authorize(Roles = "Lender,Admin")]` on controllers — it's the backend enforcing who can do what.
