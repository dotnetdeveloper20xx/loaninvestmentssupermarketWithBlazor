using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Tests.Entities;

public sealed class LenderTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsLender()
    {
        var lender = Lender.Create("Acme Corp", "John", "john@acme.com", "123456", 100_000m);

        Assert.Equal("Acme Corp", lender.CompanyName);
        Assert.Equal(100_000m, lender.AvailableFunds);
    }

    [Fact]
    public void Create_NegativeFunds_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Lender.Create("Acme", "John", "j@a.com", "123", -1m));
    }

    [Fact]
    public void DeductFunds_ValidAmount_ReducesBalance()
    {
        var lender = Lender.Create("Acme", "John", "j@a.com", "123", 50_000m);

        lender.DeductFunds(10_000m);

        Assert.Equal(40_000m, lender.AvailableFunds);
    }

    [Fact]
    public void DeductFunds_ExceedsBalance_ThrowsDomainException()
    {
        var lender = Lender.Create("Acme", "John", "j@a.com", "123", 5_000m);

        Assert.Throws<DomainException>(() => lender.DeductFunds(10_000m));
    }

    [Fact]
    public void DeductFunds_ZeroAmount_ThrowsDomainException()
    {
        var lender = Lender.Create("Acme", "John", "j@a.com", "123", 5_000m);

        Assert.Throws<DomainException>(() => lender.DeductFunds(0m));
    }

    [Fact]
    public void TopUpFunds_ValidAmount_IncreasesBalance()
    {
        var lender = Lender.Create("Acme", "John", "j@a.com", "123", 10_000m);

        lender.TopUpFunds(5_000m);

        Assert.Equal(15_000m, lender.AvailableFunds);
    }

    [Fact]
    public void TopUpFunds_ZeroAmount_ThrowsDomainException()
    {
        var lender = Lender.Create("Acme", "John", "j@a.com", "123", 10_000m);

        Assert.Throws<DomainException>(() => lender.TopUpFunds(0m));
    }
}
