namespace StockTraderBackend.PortfolioValueTracking;

public class PortfolioSnapshot {
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalValue { get; set; }
    public List<PortfolioSnapshotHolding> Holdings { get; set; } = new();

    public PortfolioSnapshot(Guid portfolioId, DateOnly date)
    {
        Id = Guid.NewGuid();
        PortfolioId = portfolioId;
        Date = date;
    }
}