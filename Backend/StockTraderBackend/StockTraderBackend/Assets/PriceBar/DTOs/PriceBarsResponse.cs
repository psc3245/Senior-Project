namespace StockTraderBackend.Assets.PriceBar.DTOs
{
    public sealed class PriceBarsResponse
    {
        public required string Symbol { get; init; }
        public required string Timeframe { get; init; }  // "1d"
        public required List<PriceBarDto> Data { get; init; } = new();
    }
}
