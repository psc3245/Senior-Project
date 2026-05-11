using StockTraderBackend.Assets.PriceBar;
using StockTraderBackend.Assets.PriceBar.Models;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.MarketData.Massive;

public sealed class DailyGroupedSyncRunner
{
    private readonly ISymbolsRepository _symbolsRepository;
    private readonly IPriceBarsRepository _priceBarsRepository;
    private readonly IMassiveMarketDataClient _massiveClient;
    private readonly ILogger<DailyGroupedSyncRunner> _logger;

    public DailyGroupedSyncRunner(
        ISymbolsRepository symbolsRepository,
        IPriceBarsRepository priceBarsRepository,
        IMassiveMarketDataClient massiveClient,
        ILogger<DailyGroupedSyncRunner> logger)
    {
        _symbolsRepository = symbolsRepository;
        _priceBarsRepository = priceBarsRepository;
        _massiveClient = massiveClient;
        _logger = logger;
    }

    public async Task RunForDateAsync(DateOnly tradingDate, CancellationToken ct = default)
    {
        var symbols = await _symbolsRepository.GetActiveSymbolsAsync(ct);

        if (symbols.Count == 0)
        {
            _logger.LogInformation("Grouped sync skipped: no active symbols.");
            return;
        }

        var symbolMap = symbols.ToDictionary(
            s => s.Ticker.Trim().ToUpperInvariant(),
            s => s.Id);

        var response = await _massiveClient.GetGroupedDailyAsync(tradingDate, ct);

        if (response?.Results is null || response.Results.Count == 0)
        {
            _logger.LogWarning("No grouped results returned for {TradingDate}", tradingDate);
            return;
        }

        var bars = new List<PriceBar>();

        foreach (var result in response.Results)
        {
            var ticker = result.Ticker.Trim().ToUpperInvariant();

            if (!symbolMap.TryGetValue(ticker, out var symbolId))
                continue;

            var utcDay = DateTimeOffset
                .FromUnixTimeMilliseconds(result.TimestampUnixMs)
                .UtcDateTime
                .Date;

            var tsUtcMidnight = DateTime.SpecifyKind(utcDay, DateTimeKind.Utc);

            bars.Add(new PriceBar
            {
                SymbolId = symbolId,
                Timeframe = "1d",
                Ts = tsUtcMidnight,
                Open = result.Open,
                High = result.High,
                Low = result.Low,
                Close = result.Close,
                Volume = result.Volume
            });
        }

        if (bars.Count == 0)
        {
            _logger.LogInformation(
                "Grouped sync for {TradingDate} returned no tracked-symbol matches.",
                tradingDate);
            return;
        }

        await _priceBarsRepository.UpsertBarsAsync(bars, ct);

        _logger.LogInformation(
            "Grouped sync complete for {TradingDate}. Upserted {Count} bars.",
            tradingDate,
            bars.Count);
    }
}