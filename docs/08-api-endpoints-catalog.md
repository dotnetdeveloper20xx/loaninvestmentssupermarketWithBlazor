# API Endpoints — The Complete Bible

## Response Format

Every endpoint returns:
```json
{
  "success": true/false,
  "message": "Human-readable message",
  "data": { ... },
  "errors": ["error1", "error2"]
}
```

## Authentication

All endpoints (except login/register) require:
```
Authorization: Bearer <jwt-access-token>
```

Tokens expire in 15 minutes. Refresh via POST `/api/auth/refresh-token`.

---

## Funding Endpoints

### GET `/api/funding/queue`
**Auth:** Lender, Admin
**Purpose:** Get approved applications available for this lender to fund
**Query params:** productTitle, minAmount, maxAmount
**Returns:** List of FundingQueueItemDto (borrower, tier, amount, term, product, rate, date)
**Pipeline:** ResourceAuthorizationBehaviour scopes to lender's products

### GET `/api/funding/{applicationId}/details`
**Auth:** Lender, Admin
**Purpose:** Full application details for funding decision
**Returns:** FundingApplicationDetailDto (borrower profile, credit tier, effective rate, purpose, approval reason)

### POST `/api/funding/{applicationId}/accept`
**Auth:** Lender, Admin
**Purpose:** Fund the loan
**Body:** None (lender resolved from auth)
**Side effects:** Deducts capital, transitions app to Funded, generates schedule, creates audit log, pushes SignalR event
**Returns:** FundingResultDto (scheduleId, EMI, totalInterest, term, rate)

### POST `/api/funding/{applicationId}/decline`
**Auth:** Lender, Admin
**Body:** `{ "applicationId": "...", "reason": "..." }`
**Purpose:** Decline funding with reason
**Side effects:** Creates audit log
**Returns:** Success message

### POST `/api/funding/top-up`
**Auth:** Lender, Admin
**Body:** `{ "amount": 50000 }`
**Purpose:** Add capital to lender account
**Validation:** Amount > 0, <= 10,000,000
**Returns:** New balance

### POST `/api/funding/{scheduleId}/restructure`
**Auth:** Lender, Admin
**Body:** `{ "newAnnualRate": 10, "newTermMonths": 36, "reason": "..." }`
**Purpose:** Restructure a distressed loan
**Guard:** Only Late/Defaulted loans can be restructured
**Returns:** RestructureResultDto (new rate, term, EMI, interest)

---

## Payment Endpoints

### POST `/api/payments/{scheduleId}/pay`
**Auth:** Any authenticated
**Body:** `{ "amount": 854.67, "paymentDate": "2026-05-24T00:00:00Z" }`
**Purpose:** Record a single payment
**Validation:** Amount > 0, date not in future
**Returns:** PaymentResultDto (installment#, status, paid, remaining)

### POST `/api/payments/{scheduleId}/pay-bulk`
**Auth:** Any authenticated
**Body:** Same as /pay
**Purpose:** Pay off multiple installments at once
**Returns:** BulkPaymentResultDto (installmentsPaid, totalApplied, remaining, isFullyPaid)

### GET `/api/payments/{scheduleId}`
**Auth:** Any authenticated
**Purpose:** Get full repayment schedule with all installments
**Returns:** RepaymentScheduleDto with List<InstallmentDto>

### GET `/api/payments/{scheduleId}/history`
**Auth:** Any authenticated
**Purpose:** Get payment history (only paid installments)
**Returns:** List of PaymentHistoryItemDto

### GET `/api/payments/{scheduleId}/export`
**Auth:** Any authenticated
**Purpose:** Download CSV of the repayment schedule
**Returns:** text/csv file download

---

## Dashboard Endpoints

### GET `/api/dashboard/summary`
**Auth:** CanViewReports policy
**Purpose:** Platform-wide summary (existing)

### GET `/api/dashboard/lender/portfolio`
**Auth:** CanViewReports
**Purpose:** Lender's portfolio KPIs
**Returns:** LenderPortfolioDto (totalFunded, activeLoans, outstanding, income, defaultRate, funds)

### GET `/api/dashboard/lender/loans?performance=&sortBy=`
**Auth:** CanViewReports
**Purpose:** Lender's funded loans list
**Returns:** List of LenderLoanDto

### GET `/api/dashboard/lender/earnings`
**Auth:** CanViewReports
**Purpose:** Interest and fee income
**Returns:** LenderEarningsDto

### GET `/api/dashboard/lender/analytics`
**Auth:** CanViewReports
**Purpose:** ROI, yield, diversification
**Returns:** InvestorAnalyticsDto with LoanBreakdown

### GET `/api/dashboard/borrower/loans`
**Auth:** CanViewReports
**Purpose:** Borrower's active loans with progress
**Returns:** List of BorrowerLoanDto

### GET `/api/dashboard/borrower/upcoming`
**Auth:** CanViewReports
**Purpose:** Payment summary + upcoming calendar
**Returns:** BorrowerPaymentSummaryDto

### GET `/api/dashboard/audit/{entityName}/{entityId}`
**Auth:** CanViewReports
**Purpose:** Audit trail for a specific entity
**Returns:** List of AuditLogDto

### GET `/api/dashboard/admin/loans?performance=&lender=`
**Auth:** Admin only
**Purpose:** Platform-wide loan overview
**Returns:** AdminLoansOverviewDto with loan list

### GET `/api/dashboard/admin/collections`
**Auth:** Admin only
**Purpose:** Defaulted loans for recovery
**Returns:** List of CollectionItemDto

---

## Notification Endpoints

### GET `/api/notifications/preferences`
**Auth:** Any authenticated
**Purpose:** Get user's notification settings
**Returns:** NotificationPreferencesDto

### PUT `/api/notifications/preferences`
**Auth:** Any authenticated
**Body:** NotificationPreferencesDto
**Purpose:** Save notification settings

---

## Infrastructure Endpoints

### GET `/health`
**Auth:** None
**Purpose:** Health check (DB connectivity)
**Returns:** "Healthy" or "Unhealthy"

### WebSocket `/hubs/loans`
**Auth:** JWT (via query string)
**Events pushed:**
- `FundingQueueChanged` → to "lenders" group
- `PaymentRecorded` → to specific user group
- `LoanFunded` → to specific user group

---

## Rate Limiting

All endpoints: 100 requests per minute per IP address.
Exceeding returns HTTP 429 Too Many Requests.

## Correlation IDs

Every request gets `X-Correlation-Id` header in the response.
Use this to trace requests through logs.
