using StockTraderBackend.Middleware;
using StockTraderBackend.PortfolioValueTracking.DTOs;
using StockTraderBackend.Portfolios;

namespace StockTraderBackend.PortfolioValueTracking;

public sealed class PortfolioSnapshotQueryService : IPortfolioSnapshotQueryService
{
    private readonly PortfolioSnapshotRepository _portfolioSnapshotRepository;
    private readonly PortfolioRepository _portfolioRepository;

    public PortfolioSnapshotQueryService(
        PortfolioSnapshotRepository portfolioSnapshotRepository,
        PortfolioRepository portfolioRepository)
    {
        _portfolioSnapshotRepository = portfolioSnapshotRepository;
        _portfolioRepository = portfolioRepository;
    }

    public async Task<PortfolioSnapshotResponse> GetSnapshotsForPortfolioAsync(
        Guid portfolioId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        var portfolio = await _portfolioRepository.getPortfolioById(portfolioId, ct);
        if (portfolio == null)
            throw new ApiException(404, $"Portfolio '{portfolioId}' not found.");

        var snapshots = await _portfolioSnapshotRepository.GetSnapshotsForPortfolioAsync(
            portfolioId, from, to, ct);

        return new PortfolioSnapshotResponse
        {
            PortfolioId = portfolioId,
            Data = snapshots.Select((s, i) =>
            {
                var prev = i > 0 ? snapshots[i - 1] : null;

                decimal? portfolioChangePercent = null;
                if (prev != null && prev.TotalValue != 0)
                    portfolioChangePercent = (s.TotalValue - prev.TotalValue) / prev.TotalValue * 100;

                var holdings = s.Holdings.Select(h =>
                {
                    decimal? priceChangePercent = null;
                    if (prev != null)
                    {
                        var prevHolding = prev.Holdings.FirstOrDefault(ph => ph.Ticker == h.Ticker);
                        if (prevHolding != null && prevHolding.PriceAtSnapshot != 0)
                            priceChangePercent = (h.PriceAtSnapshot - prevHolding.PriceAtSnapshot) / prevHolding.PriceAtSnapshot * 100;
                    }

                    return new PortfolioSnapshotHoldingDto(
                        h.Ticker,
                        h.Shares,
                        h.PriceAtSnapshot,
                        h.Value,
                        priceChangePercent
                    );
                }).ToList();

                return new PortfolioSnapshotDto(
                    s.Date,
                    s.TotalValue,
                    portfolioChangePercent,
                    holdings
                );
            }).ToList()
        };
    }
}