using System.Text.Json.Serialization;
using StockTraderBackend.Assets.Symbols.Model;

namespace StockTraderBackend.Watchlist;

public class WatchlistStock
{
    public Guid WatchlistId { get; set; }
    public int SymbolId { get; set; }
    public int Position { get; set; }
    public DateTime AddedAt { get; set; }
    
    
    [JsonIgnore]
    public Watchlist Watchlist { get; set; }
    public Symbol Symbol { get; set; }
}