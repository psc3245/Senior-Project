using StockTraderBackend.MarketData.StockSplits.Models;

namespace StockTraderBackend.Assets.Symbols.Model
{
    public class Symbol
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Exchange { get; set; } = null!;
        public AssetTypes AssetType { get; set; } = AssetTypes.Stock!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<PriceBar.Models.PriceBar> PriceBars { get; set; } = new();
        //backfill related properties
        public bool NeedsBackfill { get; set; } = true;
        public DateTime? LastBackfillAttemptUtc { get; set; }
        public DateTime? LastBackfillSuccessUtc { get; set; }

        //stocksplit related properties
        public ICollection<StockSplit> StockSplits { get; set; } = new List<StockSplit>();

        public DateTime? LastSplitScanUtc { get; set; }
        public DateTime? LastSplitDetectedUtc { get; set; }
    }

}
