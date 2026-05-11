namespace StockTraderBackend.Trades;

public static class TradeConfirmationHelper
{
    public static string BuildRequiredConfirmation(
        TradeType action,
        decimal quantity,
        string ticker)
    {
        return $"confirm {action.ToString().ToLowerInvariant()} {quantity:g} {ticker.Trim().ToUpperInvariant()}";
    }

    public static string NormalizeConfirmation(string value)
    {
        return string.Join(
            " ",
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool Matches(
        string providedConfirmation,
        TradeType action,
        decimal quantity,
        string ticker)
    {
        var required = BuildRequiredConfirmation(action, quantity, ticker);

        return string.Equals(
            NormalizeConfirmation(providedConfirmation),
            NormalizeConfirmation(required),
            StringComparison.OrdinalIgnoreCase);
    }
}