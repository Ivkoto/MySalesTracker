using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Application.Helpers;

internal static class CurrencyResolver
{
    public static Currency Resolve(IEnumerable<Currency> currencies)
    {
        var distinctCurrencies = currencies.Distinct().ToList();

        return distinctCurrencies.Count switch
        {
            0 => Currency.EUR,
            1 => distinctCurrencies[0],
            _ => throw new InvalidOperationException("Mixed currencies are not supported in a single summary.")
        };
    }
}
