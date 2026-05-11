namespace StockTraderBackend.MarketData.StockSplits.DTOs
{
    public sealed record StockSplitDto(
        int Id,
        string Ticker,
        DateOnly EffectiveDate,
        decimal SplitFrom,
        decimal SplitTo,
        decimal SplitFactor,
        string? MassiveSplitId,
        DateTime DetectedAtUtc);
}
