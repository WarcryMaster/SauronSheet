namespace SauronSheet.Domain.ValueObjects;

using System;

/// <summary>
/// Money value object representing an amount and currency.
/// Encapsulates monetary arithmetic and validation.
/// </summary>
public class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;
    public bool IsZero => Amount == 0;

    public Money(decimal amount, string currency = "EUR")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Add money to this amount (must be same currency).
    /// </summary>
    public Money Plus(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtract money from this amount (must be same currency).
    /// </summary>
    public Money Minus(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot perform operations with different currencies: {Currency} vs {other.Currency}");
    }

    public override string ToString() => $"{Amount:F2} {Currency}";

    public override bool Equals(object? obj)
    {
        return obj is Money money &&
               Amount == money.Amount &&
               Currency == money.Currency;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }
}
