using StockTraderBackend.Assets.Symbols.Model;

namespace StockTraderBackend.Assets.PriceBar.Models
{
    public class PriceBar
    {
        public int SymbolId { get; set; }
        public Symbol Symbol { get; set; } = null!;

        public string Timeframe { get; set; } = null!; // "1m", "5m", "1d"
        public DateTime Ts { get; set; }               // timestamp of the bar (UTC)

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }

}
