using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "GBP")
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        if (currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO code.");
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public bool Equals(Money? other)
    {
        if (other is null)
        {
            return false;
        }

        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public override string ToString()
    {
        return $"{Currency} {Amount:N2}";
    }
}