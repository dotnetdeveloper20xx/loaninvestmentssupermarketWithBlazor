using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.ValueObjects;

public sealed class InterestRate : IEquatable<InterestRate>
{
    private InterestRate(decimal percentage)
    {
        Percentage = percentage;
    }

    public decimal Percentage { get; }

    public static InterestRate Create(decimal percentage)
    {
        if (percentage <= 0)
        {
            throw new DomainException("Interest rate must be greater than zero.");
        }

        if (percentage > 100)
        {
            throw new DomainException("Interest rate cannot be greater than 100%.");
        }

        return new InterestRate(decimal.Round(percentage, 2));
    }

    public bool Equals(InterestRate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Percentage == other.Percentage;
    }

    public override bool Equals(object? obj)
    {
        return obj is InterestRate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Percentage.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Percentage:N2}%";
    }
}