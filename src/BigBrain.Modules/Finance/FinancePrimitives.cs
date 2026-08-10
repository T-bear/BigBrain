namespace BigBrain.Modules.Finance;

public readonly record struct Currency
{
    public Currency(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(code));
        }

        Code = normalized;
    }

    public string Code { get; }
    public override string ToString() => Code;
}

public readonly record struct Money
{
    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency.Code is null
            ? throw new ArgumentException("Currency is required.", nameof(currency))
            : currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money Round(int decimalPlaces = 2) =>
        new(decimal.Round(Amount, decimalPlaces, MidpointRounding.ToEven), Currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException("Money values with different currencies cannot be combined.");
        }
    }
}

public readonly record struct Price
{
    public Price(decimal value, Currency currency)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Price must be positive.");
        }

        Value = value;
        Currency = currency.Code is null
            ? throw new ArgumentException("Currency is required.", nameof(currency))
            : currency;
    }

    public decimal Value { get; }
    public Currency Currency { get; }
}

public readonly record struct Quantity
{
    public Quantity(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be positive.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct Percentage
{
    public Percentage(decimal value)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

public readonly record struct InstrumentId
{
    public InstrumentId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct FinanceId
{
    public FinanceId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Finance identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }
    public override string ToString() => Value.ToString("D");
}
