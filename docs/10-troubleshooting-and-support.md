# Troubleshooting & Production Support — The Complete Bible

## How to Debug Any Issue

### Step 1: Identify the Layer

| Symptom | Layer | Tool |
|---------|-------|------|
| Page won't load | Blazor | Browser DevTools → Console |
| API returns error | API/Application | Check response body errors[] |
| Data is wrong | Database | Query the table directly |
| Background job not running | Infrastructure | Check application logs |
| Auth fails | Identity | Check token expiry, roles |

### Step 2: Use Correlation IDs

Every API response includes `X-Correlation-Id` header. This same ID appears
in all server-side logs for that request. Use it to trace a request from
the browser through the API to the database.

---

## Common Issues

### "Application won't start — SQL connection error"

**Symptoms:** Exception on startup mentioning SqlConnection.

**Diagnosis:**
1. Check `appsettings.json` connection string
2. Verify SQL Server is running: `Get-Service *sql*`
3. Test connection: `sqlcmd -S .\SQLEXPRESS -Q "SELECT 1"`

**Fix:** Update connection string to match your SQL instance.

### "401 Unauthorized on every API call"

**Symptoms:** Blazor pages show no data, network tab shows 401.

**Diagnosis:**
1. Check localStorage for `accessToken`
2. Decode at jwt.io — check `exp` claim
3. If expired, check if refresh is working

**Fix:** Clear localStorage, login again. If persistent, check that
`AuthTokenHandler` is registered in Blazor Program.cs.

### "Funding fails — Lender not found"

**Symptoms:** "No lender profile found for the current user"

**Diagnosis:** The authenticated user's Identity ID doesn't match any
Lender's `UserId` column.

**Fix:** Ensure the lender record has `UserId` set to the Identity user's ID.
This happens during lender creation/linking.

### "Late payment service not working"

**Symptoms:** Overdue installments stay as "Pending" forever.

**Diagnosis:**
1. Check if `LatePaymentHostedService` is registered (it is in DI)
2. Check logs for "Late Payment Hosted Service executing daily check"
3. Verify `RepaymentSettings.GracePeriodDays` in appsettings.json
4. Check installment DueDates — are any actually past due + grace?

**Fix:** The service has a 1-minute initial delay then runs every 24 hours.
For testing, you can reduce the timer interval temporarily.

### "Amortization calculation seems wrong"

**Symptoms:** EMI doesn't match expected value.

**Diagnosis:**
1. Check the effective rate (base + credit tier adjustment)
2. Verify the formula: EMI = P × r × (1+r)^n / ((1+r)^n - 1)
3. Check if the final installment absorbed rounding

**Verification:** Sum all PrincipalPortion values — must equal FundedAmount.
Run the `AmortizationServiceTests` to confirm the engine is correct.

### "SignalR won't connect"

**Symptoms:** Real-time updates don't work, no connection in DevTools.

**Diagnosis:**
1. Check browser console for WebSocket errors
2. Verify CORS allows the Blazor origin
3. Check that `/hubs/loans` is mapped in Program.cs
4. Verify the token is being passed to the hub connection

---

## Database Investigation Queries

```sql
-- Check a lender's current state
SELECT Id, CompanyName, AvailableFunds, Status, UserId
FROM Lenders WHERE Id = '...'

-- Check a schedule's installments
SELECT InstallmentNumber, DueDate, TotalAmount, Status, PaidAmount, LateFeeAmount
FROM Installments WHERE RepaymentScheduleId = '...'
ORDER BY InstallmentNumber

-- Find overdue installments
SELECT i.*, rs.LenderId
FROM Installments i
JOIN RepaymentSchedules rs ON i.RepaymentScheduleId = rs.Id
WHERE i.Status IN (1, 3) -- Pending or PartiallyPaid
AND i.DueDate < DATEADD(DAY, -5, GETUTCDATE()) -- Past grace period

-- Check audit trail for an application
SELECT Action, Description, PerformedBy, OccurredAtUtc
FROM AuditLogs
WHERE EntityName = 'LoanApplication' AND EntityId = '...'
ORDER BY OccurredAtUtc DESC

-- Platform health check
EXEC sp_GetPlatformSummary
```

---

## Performance Monitoring

### Slow Queries
The `PerformanceBehaviour` logs a warning if any handler takes > 500ms.
Check logs for "Long Running Request" messages.

### Memory Usage
The `IMemoryCache` caches dashboard results for 2 minutes. If memory grows
unbounded, check cache entry count and consider setting size limits.

### Background Service Health
The `LatePaymentHostedService` logs at INFO level when it starts and
completes each daily run. If you don't see these logs, the service crashed.
