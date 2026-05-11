namespace StockTraderBackend.Watchlist.DTOs
{
    public sealed class WatchlistWithPricesDto
    {
        public Guid WatchlistId { get; set; }
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }

        public List<WatchlistStockWithPriceDto> Stocks { get; set; } = [];
    }
}
