namespace StockTraderBackend.Watchlist.DTOs
{
    public sealed class WatchlistStockWithPriceDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string? Name { get; set; }

        public decimal? CurrentPrice { get; set; }
    }
}
