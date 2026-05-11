namespace StockTraderBackend.PortfolioValueTracking.DTOs
{
    public sealed class PortfolioSnapshotResponse
    {
        public required Guid PortfolioId { get; init; }
        public required List<PortfolioSnapshotDto> Data { get; init; } = new();
    }
}