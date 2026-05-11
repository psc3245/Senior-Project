namespace StockTraderBackend.PortfolioValueTracking.DTOs
{
    public sealed record PortfolioSnapshotHoldingDto(
        string Ticker,
        decimal Shares,
        decimal PriceAtSnapshot,
        decimal Value,
        decimal? PriceChangePercent
    );

    public sealed record PortfolioSnapshotDto(
        DateOnly Date,
        decimal TotalValue,
        decimal? PortfolioChangePercent,
        List<PortfolioSnapshotHoldingDto> Holdings
    );
}