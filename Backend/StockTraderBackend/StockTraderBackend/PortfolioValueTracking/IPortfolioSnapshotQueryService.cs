using StockTraderBackend.PortfolioValueTracking.DTOs;

namespace StockTraderBackend.PortfolioValueTracking
{
    public interface IPortfolioSnapshotQueryService
    {
        Task<PortfolioSnapshotResponse> GetSnapshotsForPortfolioAsync(Guid portfolioId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    }
}