using StockTraderBackend.Assets;

namespace StockTraderBackend.PortfolioValueTracking;

public class PortfolioSnapshotHolding
{
    public Guid Id { get; set; }
    public Guid PortfolioSnapshotId { get; set; }
    public string Ticker { get; set; } = null!;
    public decimal Shares { get; set; }
    public decimal PriceAtSnapshot { get; set; }
    public decimal Value { get; set; } // Shares * PriceAtSnapshot

    public PortfolioSnapshotHolding(Guid portfolioSnapshotId, string ticker, decimal shares, decimal priceAtSnapshot)
    {
        Id = Guid.NewGuid();
        PortfolioSnapshotId = portfolioSnapshotId;
        Ticker = ticker;
        Shares = shares;
        PriceAtSnapshot = priceAtSnapshot;
        Value = priceAtSnapshot * shares;
    }
}