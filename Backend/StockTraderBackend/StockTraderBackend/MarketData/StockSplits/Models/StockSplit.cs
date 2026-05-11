using StockTraderBackend.Assets.Symbols.Model;

namespace StockTraderBackend.MarketData.StockSplits.Models
{
    public sealed class StockSplit
    {
        public int Id { get; set; }

        public int SymbolId { get; set; }
        public Symbol Symbol { get; set; } = null!;

        public DateOnly EffectiveDate { get; set; }

        // Example: 4-for-1 split
        public decimal SplitFrom { get; set; }
        public decimal SplitTo { get; set; }

        // Example: 4-for-1 => 4.0
        public decimal SplitFactor { get; set; }

        public string? MassiveSplitId { get; set; }

        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
