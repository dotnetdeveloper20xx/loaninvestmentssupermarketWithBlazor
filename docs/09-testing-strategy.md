# Testing Strategy — The Complete Bible

## Test Projects

```
tests/
├── LoanSuperMarket.Domain.Tests/      ← xUnit, no mocking needed
├── LoanSuperMarket.Application.Tests/ ← xUnit + Moq
└── LoanSuperMarket.Api.Tests/         ← xUnit + WebApplicationFactory
```

---

## Domain Tests — What We Test and Why

### `Entities/LenderTests.cs` (7 tests)

**`Create_ValidInputs_ReturnsLender`**
- Creates a lender with valid data
- Asserts CompanyName and AvailableFunds are set correctly
- Proves the factory method works

**`Create_NegativeFunds_ThrowsDomainException`**
- Tries to create with -1 funds
- Proves the validation catches negative amounts

**`DeductFunds_ValidAmount_ReducesBalance`**
- Creates with 50,000, deducts 10,000
- Asserts balance is 40,000
- Proves basic arithmetic is correct

**`DeductFunds_ExceedsBalance_ThrowsDomainException`**
- Creates with 5,000, tries to deduct 10,000
- Proves the insufficient funds guard works

**`DeductFunds_ZeroAmount_ThrowsDomainException`**
- Tries to deduct 0
- Proves zero is rejected

**`TopUpFunds_ValidAmount_IncreasesBalance`**
- Creates with 10,000, tops up 5,000
- Asserts balance is 15,000

**`TopUpFunds_ZeroAmount_ThrowsDomainException`**
- Tries to top up 0
- Proves zero is rejected

### `Entities/InstallmentTests.cs` (10 tests)

Tests every state transition in the installment state machine:
- Full payment → Paid
- Double payment → throws
- Partial payment → PartiallyPaid
- Partial payment completing total → Paid
- Overpayment → throws
- Zero payment → throws
- MarkLate from Pending → Late (with fee calculation)
- MarkLate from Paid → throws
- MarkMissed from Late → Missed
- MarkMissed from Pending → throws

### `Services/PaymentProcessorTests.cs` (5 tests)

Tests the domain service that coordinates payments:
- Single payment pays next installment
- Zero amount throws
- Exceeding amount throws
- Bulk payment pays multiple installments
- Bulk payment with exact total pays all

---

## Application Tests — What We Test and Why

### `Features/AmortizationServiceTests.cs` (8 tests)

**`GenerateSchedule_ValidInputs_ReturnsCorrectInstallmentCount`**
- 12-month term → 12 installments
- Proves the loop generates the right count

**`GenerateSchedule_PrincipalSumsToFundedAmount`**
- Sum of all principal portions = funded amount (within rounding tolerance)
- This is the CRITICAL invariant — money must balance

**`GenerateSchedule_EmiIsConsistent`**
- All installments have approximately the same total amount
- Proves the EMI formula produces consistent payments

**`GenerateSchedule_FinalInstallmentHasZeroRemainingBalance`**
- Last installment's remaining balance = 0
- Proves the loan is fully amortized

**`GenerateSchedule_ZeroPrincipal_ThrowsDomainException`**
- Can't generate a schedule for £0

**`GenerateSchedule_ZeroRate_ThrowsDomainException`**
- Can't generate with 0% rate

**`GenerateSchedule_ZeroTerm_ThrowsDomainException`**
- Can't generate for 0 months

**`GenerateSchedule_DueDatesAreSequentialMonths`**
- Funding on Jan 15 → due dates are Feb 15, Mar 15, Apr 15...
- Proves date arithmetic is correct

---

## Integration Tests (Api.Tests)

Uses `WebApplicationFactory<Program>` to test the full HTTP pipeline:
- Middleware (correlation IDs, exception handling)
- Authentication and authorization
- Controller → MediatR → Handler → Database
- Response format (ApiResponse<T>)

---

## How to Run Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/LoanSuperMarket.Domain.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## What Makes These Tests Valuable

1. **Domain tests prove business rules** — If someone changes the Installment
   entity and breaks a state transition, the test fails immediately.

2. **Amortization tests prove financial correctness** — The principal invariant
   test ensures money always balances. This is auditable evidence.

3. **They're fast** — Domain tests have zero dependencies. They run in
   milliseconds. No database, no HTTP, no setup.

4. **They document behaviour** — Reading the test names tells you exactly what
   the system does and doesn't allow.
