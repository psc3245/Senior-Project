using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.MarketData.Massive;
using StockTraderBackend.MarketData.StockSplits.Models;
using StockTraderBackend.MarketData.StockSplits.Repository;

namespace StockTraderBackend.MarketData.StockSplits.Service
{
    public sealed class StockSplitDetectionService : IStockSplitDetectionService
    {
        private readonly IMassiveCorporateActionsClient _massiveCorporateActionsClient;
        private readonly IStockSplitsRepository _stockSplitsRepository;
        private readonly ISymbolsRepository _symbolsRepository;
        private readonly ILogger<StockSplitDetectionService> _logger;

        public StockSplitDetectionService(
            IMassiveCorporateActionsClient massiveCorporateActionsClient,
            IStockSplitsRepository stockSplitsRepository,
            ISymbolsRepository symbolsRepository,
            ILogger<StockSplitDetectionService> logger)
        {
            _massiveCorporateActionsClient = massiveCorporateActionsClient;
            _stockSplitsRepository = stockSplitsRepository;
            _symbolsRepository = symbolsRepository;
            _logger = logger;
        }

        public async Task<int> ScanRecentSplitsAsync(
            int lookbackDays,
            CancellationToken ct = default)
        {
            if (lookbackDays <= 0)
                throw new ArgumentOutOfRangeException(nameof(lookbackDays));

            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = todayUtc.AddDays(-lookbackDays);
            var toDate = todayUtc.AddDays(-1);

            if (toDate < fromDate)
                return 0;

            var splits = await _massiveCorporateActionsClient.GetSplitsAsync(fromDate, toDate, ct);

            var trackedSymbols = await _symbolsRepository.GetActiveSymbolsByTickersAsync(
                splits.Select(x => x.Ticker),
                ct);

            var symbolsByTicker = trackedSymbols.ToDictionary(
                x => x.Ticker,
                x => x,
                StringComparer.OrdinalIgnoreCase);

            var touchedSymbolIds = new HashSet<int>();
            var newSplitCount = 0;

            foreach (var split in splits)
            {
                if (split.ExecutionDate >= todayUtc)
                    continue;

                if (!symbolsByTicker.TryGetValue(split.Ticker, out var symbol))
                    continue;

                touchedSymbolIds.Add(symbol.Id);

                var alreadyExists =
                    !string.IsNullOrWhiteSpace(split.Id)
                        ? await _stockSplitsRepository.ExistsByMassiveSplitIdAsync(split.Id, ct)
                        : await _stockSplitsRepository.ExistsBySymbolAndDateAsync(
                            symbol.Id,
                            split.ExecutionDate,
                            ct);

                if (alreadyExists)
                    continue;

                var splitFactor = split.SplitFrom == 0
                    ? 0
                    : split.SplitTo / split.SplitFrom;

                var stockSplit = new StockSplit
                {
                    SymbolId = symbol.Id,
                    EffectiveDate = split.ExecutionDate,
                    SplitFrom = split.SplitFrom,
                    SplitTo = split.SplitTo,
                    SplitFactor = splitFactor,
                    MassiveSplitId = string.IsNullOrWhiteSpace(split.Id) ? null : split.Id,
                    DetectedAtUtc = DateTime.UtcNow
                };

                await _stockSplitsRepository.AddAsync(stockSplit, ct);
                await _stockSplitsRepository.SaveChangesAsync(ct);

                await _symbolsRepository.MarkNeedsBackfillAsync(symbol.Id, ct);
                await _symbolsRepository.MarkSplitDetectedAsync(symbol.Id, ct);

                newSplitCount++;

                _logger.LogInformation(
                    "Detected new stock split for {Ticker} on {ExecutionDate}. Marked symbol for backfill.",
                    symbol.Ticker,
                    split.ExecutionDate);
            }

            foreach (var symbolId in touchedSymbolIds)
            {
                await _symbolsRepository.MarkSplitScanUtcAsync(symbolId, ct);
            }

            return newSplitCount;
        }
        public async Task<IReadOnlyList<StockSplit>> GetRecentStoredSplitsAsync(
            int lookbackDays = 30,
            int limit = 50,
            CancellationToken ct = default)
        {
            lookbackDays = Math.Clamp(lookbackDays, 1, 365);
            limit = Math.Clamp(limit, 1, 100);

            var to = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = to.AddDays(-lookbackDays);

            return await _stockSplitsRepository.GetRecentSplitsAsync(
                from,
                to,
                limit,
                ct
            );
        }

        public async Task<IReadOnlyList<StockSplit>> GetSplitsForTickerAsync(
            string ticker,
            CancellationToken ct = default)
        {
            var symbol = await _symbolsRepository.GetByTickerAsync(ticker, ct);
            if (symbol is null)
                return Array.Empty<StockSplit>();

            return await _stockSplitsRepository.GetBySymbolIdAsync(symbol.Id, ct);
        }

        public async Task<bool> DeleteSplitAsync(
            int splitId,
            CancellationToken ct = default)
        {
            var split = await _stockSplitsRepository.GetByIdAsync(splitId, ct);
            if (split is null)
                return false;

            _stockSplitsRepository.Remove(split);
            await _stockSplitsRepository.SaveChangesAsync(ct);
            return true;
        }
    }
}
