using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Tests.Entities;

public sealed class InstallmentTests
{
    private static Installment CreatePendingInstallment(decimal principal = 500m, decimal interest = 100m)
    {
        return new Installment(1, DateTime.UtcNow.AddDays(30), principal, interest, 9500m);
    }

    [Fact]
    public void RecordFullPayment_SetsStatusToPaid()
    {
        var installment = CreatePendingInstallment();

        installment.RecordFullPayment(DateTime.UtcNow);

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
        Assert.Equal(600m, installment.PaidAmount);
        Assert.NotNull(installment.PaidDate);
    }

    [Fact]
    public void RecordFullPayment_WhenAlreadyPaid_ThrowsDomainException()
    {
        var installment = CreatePendingInstallment();
        installment.RecordFullPayment(DateTime.UtcNow);

        Assert.Throws<DomainException>(() => installment.RecordFullPayment(DateTime.UtcNow));
    }

    [Fact]
    public void RecordPartialPayment_SetsStatusToPartiallyPaid()
    {
        var installment = CreatePendingInstallment();

        installment.RecordPartialPayment(300m, DateTime.UtcNow);

        Assert.Equal(InstallmentStatus.PartiallyPaid, installment.Status);
        Assert.Equal(300m, installment.PaidAmount);
    }

    [Fact]
    public void RecordPartialPayment_FullAmount_SetsStatusToPaid()
    {
        var installment = CreatePendingInstallment();

        installment.RecordPartialPayment(600m, DateTime.UtcNow);

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
    }

    [Fact]
    public void RecordPartialPayment_ExceedsTotal_ThrowsDomainException()
    {
        var installment = CreatePendingInstallment();

        Assert.Throws<DomainException>(() =>
            installment.RecordPartialPayment(700m, DateTime.UtcNow));
    }

    [Fact]
    public void RecordPartialPayment_ZeroAmount_ThrowsDomainException()
    {
        var installment = CreatePendingInstallment();

        Assert.Throws<DomainException>(() =>
            installment.RecordPartialPayment(0m, DateTime.UtcNow));
    }

    [Fact]
    public void MarkLate_FromPending_TransitionsToLate()
    {
        var installment = CreatePendingInstallment();

        installment.MarkLate(0.02m);

        Assert.Equal(InstallmentStatus.Late, installment.Status);
        Assert.Equal(12m, installment.LateFeeAmount); // 600 * 0.02
    }

    [Fact]
    public void MarkLate_FromPaid_ThrowsDomainException()
    {
        var installment = CreatePendingInstallment();
        installment.RecordFullPayment(DateTime.UtcNow);

        Assert.Throws<DomainException>(() => installment.MarkLate(0.02m));
    }

    [Fact]
    public void MarkMissed_FromLate_TransitionsToMissed()
    {
        var installment = CreatePendingInstallment();
        installment.MarkLate(0.02m);

        installment.MarkMissed();

        Assert.Equal(InstallmentStatus.Missed, installment.Status);
    }

    [Fact]
    public void MarkMissed_FromPending_ThrowsDomainException()
    {
        var installment = CreatePendingInstallment();

        Assert.Throws<DomainException>(() => installment.MarkMissed());
    }
}
