namespace StockTraderBackend.Watchlist.DTOs
{
    public class AddStockRequest
    {
        public int? SymbolId { get; set; }
        public string? Ticker { get; set; }
    }
}