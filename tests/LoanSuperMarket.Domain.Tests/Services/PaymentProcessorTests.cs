using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.Services;

namespace LoanSuperMarket.Domain.Tests.Services;

public sealed class PaymentProcessorTests
{
    private static RepaymentSchedule CreateScheduleWithInstallments(int count = 3)
    {
        var schedule = new RepaymentSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 30_000m, 12m, count, 1000m, 6000m);

        for (var i = 1; i <= count; i++)
        {
            schedule.AddInstallment(new Installment(
                i, DateTime.UtcNow.AddMonths(i), 800m, 200m, 30_000m - (800m * i)));
        }

        return schedule;
    }

    [Fact]
    public void RecordPayment_PaysNextPendingInstallment()
    {
        var schedule = CreateScheduleWithInstallments();
        var processor = new PaymentProcessor();

        processor.RecordPayment(schedule, 1000m, DateTime.UtcNow);

        var first = schedule.Installments.First(i => i.InstallmentNumber == 1);
        Assert.Equal(InstallmentStatus.Paid, first.Status);
    }

    [Fact]
    public void RecordPayment_ZeroAmount_ThrowsDomainException()
    {
        var schedule = CreateScheduleWithInstallments();
        var processor = new PaymentProcessor();

        Assert.Throws<DomainException>(() =>
            processor.RecordPayment(schedule, 0m, DateTime.UtcNow));
    }

    [Fact]
    public void RecordPayment_ExceedsOwed_ThrowsDomainException()
    {
        var schedule = CreateScheduleWithInstallments();
        var processor = new PaymentProcessor();

        Assert.Throws<DomainException>(() =>
            processor.RecordPayment(schedule, 5000m, DateTime.UtcNow));
    }

    [Fact]
    public void RecordBulkPayment_PaysMultipleInstallments()
    {
        var schedule = CreateScheduleWithInstallments();
        var processor = new PaymentProcessor();

        var paid = processor.RecordBulkPayment(schedule, 2500m, DateTime.UtcNow);

        Assert.Equal(2, paid);
        Assert.Equal(InstallmentStatus.Paid, schedule.Installments.First(i => i.InstallmentNumber == 1).Status);
        Assert.Equal(InstallmentStatus.Paid, schedule.Installments.First(i => i.InstallmentNumber == 2).Status);
        Assert.Equal(InstallmentStatus.PartiallyPaid, schedule.Installments.First(i => i.InstallmentNumber == 3).Status);
    }

    [Fact]
    public void RecordBulkPayment_PaysAllInstallments()
    {
        var schedule = CreateScheduleWithInstallments();
        var processor = new PaymentProcessor();

        var paid = processor.RecordBulkPayment(schedule, 3000m, DateTime.UtcNow);

        Assert.Equal(3, paid);
        Assert.Null(schedule.GetNextPendingInstallment());
    }
}
