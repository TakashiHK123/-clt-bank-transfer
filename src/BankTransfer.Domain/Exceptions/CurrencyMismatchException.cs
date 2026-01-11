namespace BankTransfer.Domain.Exceptions;

public sealed class CurrencyMismatchException : Exception
{
    public string FromCurrency { get; }
    public string ToCurrency { get; }

    public CurrencyMismatchException(string fromCurrency, string toCurrency)
        : base($"Currency mismatch: '{fromCurrency}' -> '{toCurrency}'. Transfers must use the same currency.")
    {
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
    }
}
