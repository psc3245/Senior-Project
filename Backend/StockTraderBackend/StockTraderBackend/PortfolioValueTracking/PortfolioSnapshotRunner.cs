using StockTraderBackend.Portfolios;
using StockTraderBackend.StockAPI;

namespace StockTraderBackend.PortfolioValueTracking;

public class PortfolioSnapshotRunner
{
    private readonly ILogger<PortfolioSnapshotRunner> _logger;
    private readonly PortfolioSnapshotRepository _portfolioSnapshotRepository;
    private readonly PortfolioRepository _portfolioRepository;
    private readonly IStockAPIService _stockAPIService;

    public PortfolioSnapshotRunner(ILogger<PortfolioSnapshotRunner> logger,
        PortfolioSnapshotRepository portfolioSnapshotRepository, PortfolioRepository portfolioRepository, IStockAPIService stockAPIService)
    {
        _logger = logger;
        _portfolioSnapshotRepository = portfolioSnapshotRepository;
        _portfolioRepository = portfolioRepository;
        _stockAPIService = stockAPIService;
    }

    public async Task UpsertAllPortfolioSnapshots(DateOnly date, CancellationToken ct = default)
    {
        var portfolios = await _portfolioRepository.getPortfolios(ct);
        
        if (portfolios.Count == 0)
        {
            _logger.LogInformation("No portfolios found");
            return;
        }
        
        HashSet<string> uniqueTickers = new HashSet<string>();
        foreach (var p in  portfolios)
        {
            foreach (var h in p.holdings)
            {
                uniqueTickers.Add(h.ticker);
            }
        }
        
        Dictionary<string, decimal> tickersAndPrices = new Dictionary<string, decimal>();

        foreach (var s in uniqueTickers)
        {
            var price = await _stockAPIService.getPriceByTickerAsync(s);
            if (price == null)
            {
                _logger.LogError($"ERR: No ticker {s}");
            }
            else tickersAndPrices[s] = price.Value;
        }

        foreach (var p in portfolios)
        {
            PortfolioSnapshot portfolioSnapshot = new PortfolioSnapshot(p.portfolioId, date);
            foreach (var h in p.holdings)
            {
                if (!tickersAndPrices.ContainsKey(h.ticker)) continue;
                PortfolioSnapshotHolding portfolioSnapshotHolding = new PortfolioSnapshotHolding(portfolioSnapshot.PortfolioId, h.ticker, h.quantity, tickersAndPrices[h.ticker]);
                portfolioSnapshot.Holdings.Add(portfolioSnapshotHolding);
            }
            portfolioSnapshot.TotalValue = portfolioSnapshot.Holdings.Sum(h => h.Value) + p.cashBalance;
            await _portfolioSnapshotRepository.UpsertPortfolioSnapshot(portfolioSnapshot, ct);
        }
    }
    
}