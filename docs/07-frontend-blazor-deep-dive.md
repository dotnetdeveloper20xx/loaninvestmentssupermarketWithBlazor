# Frontend (Blazor WASM) — The Complete Bible

## How It Works

Blazor WebAssembly downloads the .NET runtime + our app DLLs to the browser.
C# code runs directly in the browser via WebAssembly. No JavaScript needed
for business logic.

The app is a Single Page Application (SPA):
- One HTML page loads initially
- Navigation happens client-side (no page reloads)
- API calls happen via HttpClient
- UI updates reactively when state changes

---

## The MainLayout

Location: `Layout/MainLayout.razor`

This is the shell that wraps every page. It contains:
- **Left sidebar** — Role-based navigation links
- **Top header** — User info, theme toggle, logout
- **Content area** — Where `@Body` renders the current page
- **Infrastructure** — ToastContainer, ModalHost, DrawerHost, ErrorBoundary

The sidebar uses `<AuthorizeView Roles="...">` to show/hide links based on
the user's roles. A Borrower never sees "Funding Queue". A Lender never sees
"My Applications".

---

## Authentication Flow

1. User logs in → API returns JWT access token + refresh token
2. `JwtAuthenticationStateProvider` stores both in localStorage
3. Parses JWT claims → builds ClaimsPrincipal
4. `AuthorizeView` components use this to show/hide UI
5. `AuthTokenHandler` (DelegatingHandler) attaches Bearer token to every HTTP request
6. Background timer checks expiry every 30 seconds
7. 2 minutes before expiry → auto-refreshes using refresh token
8. On 401 → attempts refresh before failing

---

## Page Pattern (Every Page Follows This)

```razor
@page "/route"
@attribute [Authorize(Roles = "...")]
@inject SomeApiClient ApiClient
@inject ToastService ToastService

<PageHeader Title="..." Subtitle="...">
    <ActionContent>buttons here</ActionContent>
</PageHeader>

@if (_isLoading)
{
    <LoadingSkeleton Variant="table" />
}
else if (_data.Count == 0)
{
    <EmptyState Icon="..." Title="..." Message="..." />
}
else
{
    <!-- data display -->
}

@code {
    private List<SomeDto> _data = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var response = await ApiClient.GetDataAsync();
        if (response?.Success == true)
            _data = response.Data?.ToList() ?? [];
        _isLoading = false;
    }
}
```

---

## Reusable Component Library (30+ Components)

### Layout & Structure
- `PageHeader` — Title + subtitle + action slot
- `AppDataTable` — Table with loading/empty states
- `AppErrorBoundary` — Catches exceptions, shows recovery UI

### Data Display
- `StatCard` — Metric card with color schemes and trends
- `Badge` — Colored status pill
- `ProgressBar` — Animated bar with label
- `StatusBadge` — Maps status strings to badge colors
- `BarChart` — Pure CSS vertical bar chart
- `DonutChart` — CSS conic-gradient ring chart
- `Sparkline` — SVG polyline mini-chart
- `AnimatedCounter` — Numbers count up on load
- `PaymentTimeline` — Colored blocks per installment status

### Interactive
- `FaqItem` — Collapsible accordion
- `ThemeToggle` — Dark/light mode switch
- `LoanCalculatorWidget` — EMI calculator with live results
- `AffordabilityCheck` — Income vs expenses analysis
- `VirtualizedList<T>` — Virtualized rendering for large lists

### Modals & Forms
- `TopUpFundsModal` — Add capital
- `RestructureModal` — Change loan terms
- `FundingDecision` — Accept/decline funding
- `PaymentForm` — Record a payment

### Loading & Empty States
- `LoadingSkeleton` — Animated placeholders (cards/table/content)
- `EmptyState` — Icon + message + optional action

---

## API Client Pattern

Each domain area has a typed client:
- `FundingApiClient` — Funding queue, accept, decline, top-up, restructure
- `PaymentsApiClient` — Record payment, bulk payment, get schedule, history
- `DashboardApiClient` — All dashboard endpoints (lender, borrower, admin)
- `LendersApiClient` — Lender CRUD
- `BorrowersApiClient` — Borrower CRUD
- `LoanApplicationsApiClient` — Application workflow
- `AuthApiClient` — Login, register, refresh, logout

All return `ApiResponse<T>?` — nullable because network calls can fail.

---

## SignalR Client

`LoanHubClient` connects to `/hubs/loans` on the API:
- Connects automatically on page load (MainLayout.OnAfterRenderAsync)
- Uses the JWT token for authentication
- Subscribes to: FundingQueueChanged, PaymentRecorded, LoanFunded
- Auto-reconnects on disconnect
- Stops on logout

---

## Dark Mode

`ThemeService`:
- Reads preference from localStorage
- Toggles `dark` class on `<html>` element
- Persists choice
- Components use Tailwind's `dark:` variants for styling

---

## Key Pages Explained

### `/funding-queue` (FundingQueue.razor)
- Shows lender's available capital at top
- Filters: product title, min/max amount
- Table: borrower, tier, amount, term, product, rate, date
- "View Details" opens FundingDecision modal
- After funding: toast + balance refresh + queue refresh

### `/repayment-schedule/{id}` (RepaymentSchedule.razor)
- Summary cards: funded amount, rate, term, EMI, total interest
- PaymentTimeline: visual blocks per installment
- Action buttons: Restructure Loan, Pay All Remaining
- Installment table: #, date, principal, interest, total, balance, status, paid, fee, action
- "Pay" button on next pending installment
- CSV export link
- Audit trail timeline at bottom

### `/lender-dashboard` (LenderDashboard.razor)
- 5 tabs: Portfolio, Loans, Earnings, Analytics, Comparison
- Each tab renders a dedicated component
- Portfolio: stat cards with top-up modal
- Loans: filterable table with performance badges
- Earnings: interest received, projected returns, late fees
- Analytics: ROI per loan, yield, diversification score
- Comparison: grouped performance stats + bar chart

### `/borrower-dashboard` (BorrowerDashboard.razor)
- 5 tabs: Applications, Active Loans, Payment History, Upcoming, Calculator
- Applications: existing loan application list
- Active Loans: progress bars, due-soon warnings
- Payment History: table of past payments
- Upcoming: next 3 months calendar
- Calculator: EMI tool + affordability check
