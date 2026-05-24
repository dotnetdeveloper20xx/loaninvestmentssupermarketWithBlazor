using LoanSuperMarket.Application.Features.Funding;
using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Application.Tests.Features;

public sealed class AmortizationServiceTests
{
    private readonly AmortizationService _service = new();

    [Fact]
    public void GenerateSchedule_ValidInputs_ReturnsCorrectInstallmentCount()
    {
        var schedule = _service.GenerateSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 10_000m, 12m, 12, DateTime.UtcNow);

        Assert.Equal(12, schedule.Installments.Count);
    }

    [Fact]
    public void GenerateSchedule_PrincipalSumsToFundedAmount()
    {
        var schedule = _service.GenerateSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 25_000m, 10m, 24, DateTime.UtcNow);

        var totalPrincipal = schedule.Installments.Sum(i => i.PrincipalPortion);

        // Should equal funded amount (rounding tolerance)
        Assert.InRange(totalPrincipal, 24_999.99m, 25_000.01m);
    }

    [Fact]
    public void GenerateSchedule_EmiIsConsistent()
    {
        var schedule = _service.GenerateSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 10_000m, 12m, 12, DateTime.UtcNow);

        // All installments should have the same total (principal + interest) approximately
        var emis = schedule.Installments.Select(i => i.TotalAmount).Distinct().ToList();
        // Due to rounding, last installment may differ slightly
        Assert.InRange(emis.Count, 1, 2);
    }

    [Fact]
    public void GenerateSchedule_FinalInstallmentHasZeroRemainingBalance()
    {
        var schedule = _service.GenerateSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 50_000m, 14m, 36, DateTime.UtcNow);

        var lastInstallment = schedule.Installments.OrderBy(i => i.InstallmentNumber).Last();

        Assert.Equal(0m, lastInstallment.RemainingBalance);
    }

    [Fact]
    public void GenerateSchedule_ZeroPrincipal_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            _service.GenerateSchedule(Guid.NewGuid(), Guid.NewGuid(), 0m, 12m, 12, DateTime.UtcNow));
    }

    [Fact]
    public void GenerateSchedule_ZeroRate_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            _service.GenerateSchedule(Guid.NewGuid(), Guid.NewGuid(), 10_000m, 0m, 12, DateTime.UtcNow));
    }

    [Fact]
    public void GenerateSchedule_ZeroTerm_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            _service.GenerateSchedule(Guid.NewGuid(), Guid.NewGuid(), 10_000m, 12m, 0, DateTime.UtcNow));
    }

    [Fact]
    public void GenerateSchedule_DueDatesAreSequentialMonths()
    {
        var fundingDate = new DateTime(2024, 1, 15);
        var schedule = _service.GenerateSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 10_000m, 12m, 6, fundingDate);

        var installments = schedule.Installments.OrderBy(i => i.InstallmentNumber).ToList();

        Assert.Equal(new DateTime(2024, 2, 15), installments[0].DueDate);
        Assert.Equal(new DateTime(2024, 3, 15), installments[1].DueDate);
        Assert.Equal(new DateTime(2024, 7, 15), installments[5].DueDate);
    }
}
