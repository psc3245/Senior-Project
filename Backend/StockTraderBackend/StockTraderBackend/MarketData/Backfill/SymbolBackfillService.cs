using System.Net;
using Microsoft.Extensions.Options;
using StockTraderBackend.Assets.PriceBar;
using StockTraderBackend.Assets.PriceBar.Models;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.MarketData.Calendar;
using StockTraderBackend.MarketData.Massive;

namespace StockTraderBackend.MarketData.Backfill
{
    public sealed class SymbolBackfillService : ISymbolBackfillService
    {
        private const string DailyTimeframe = "1d";

        private readonly ISymbolsRepository _symbolsRepository;
        private readonly IPriceBarsRepository _priceBarsRepository;
        private readonly IMassiveMarketDataClient _massiveClient;
        private readonly IMarketCalendarService _marketCalendarService;
        private readonly ILogger<SymbolBackfillService> _logger;
        private readonly BackfillOptions _options;

        public SymbolBackfillService(
            ISymbolsRepository symbolsRepository,
            IPriceBarsRepository priceBarsRepository,
            IMassiveMarketDataClient massiveClient,
            IMarketCalendarService marketCalendarService,
            IOptions<BackfillOptions> options,
            ILogger<SymbolBackfillService> logger)
        {
            _symbolsRepository = symbolsRepository;
            _priceBarsRepository = priceBarsRepository;
            _massiveClient = massiveClient;
            _marketCalendarService = marketCalendarService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task BackfillSymbolAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _symbolsRepository.GetByIdAsync(symbolId, ct);

            if (symbol == null)
                return;

            await _symbolsRepository.MarkBackfillAttemptAsync(symbolId, ct);

            try
            {
                // Massive daily data is only expected through yesterday, not today.
                var latestExpectedTradingDay =
                    _marketCalendarService.GetMostRecentTradingDay(DateTime.UtcNow.Date.AddDays(-1));

                var desiredOldestTradingDay =
                    latestExpectedTradingDay.AddYears(-_options.BackfillYears);

                _logger.LogInformation(
                    "Backfilling full range for {Ticker} from {From} to {To}",
                    symbol.Ticker,
                    DateOnly.FromDateTime(desiredOldestTradingDay),
                    DateOnly.FromDateTime(latestExpectedTradingDay));

                var bars =
                    await ExecuteWithRetryAsync(
                        async token =>
                        {
                            var response =
                                await _massiveClient.GetDailyBarsAsync(
                                    symbol.Ticker,
                                    DateOnly.FromDateTime(desiredOldestTradingDay),
                                    DateOnly.FromDateTime(latestExpectedTradingDay),
                                    token);

                            _logger.LogInformation(
                                "Massive returned {Count} bars for {Ticker} from {From} to {To}",
                                response?.Results?.Count ?? 0,
                                symbol.Ticker,
                                DateOnly.FromDateTime(desiredOldestTradingDay),
                                DateOnly.FromDateTime(latestExpectedTradingDay));

                            if (response?.Results != null)
                            {
                                foreach (var r in response.Results)
                                {
                                    var rawUtc =
                                        DateTimeOffset
                                            .FromUnixTimeMilliseconds(r.TimestampUnixMs)
                                            .UtcDateTime;

                                    var normalized = rawUtc.Date;
                                }
                            }

                            if (response?.Results == null || response.Results.Count == 0)
                            {
                                return new List<PriceBar>();
                            }

                            return response.Results
                                .Select(r =>
                                {
                                    var utcDay = DateTimeOffset
                                        .FromUnixTimeMilliseconds(r.TimestampUnixMs)
                                        .UtcDateTime
                                        .Date;

                                    var utcMidnight = DateTime.SpecifyKind(utcDay, DateTimeKind.Utc);

                                    return new PriceBar
                                    {
                                        SymbolId = symbol.Id,
                                        Timeframe = DailyTimeframe,
                                        Ts = utcMidnight,
                                        Open = r.Open,
                                        High = r.High,
                                        Low = r.Low,
                                        Close = r.Close,
                                        Volume = r.Volume
                                    };
                                })
                                .ToList();
                        },
                        ct);

                if (bars.Count > 0)
                {
                    await _priceBarsRepository.UpsertBarsAsync(bars, ct);
                }
                else
                {
                    _logger.LogInformation(
                        "No bars returned for {Ticker} during full backfill range.",
                        symbol.Ticker);
                }


                await _priceBarsRepository.DeleteBarsOlderThanAsync(
                    symbolId,
                    "1d",
                    desiredOldestTradingDay,
                    ct);

                await _symbolsRepository.MarkBackfillSuccessAsync(symbolId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Backfill failed for symbol id {SymbolId}",
                    symbolId);

                throw;
            }
        }

        public async Task ScanAndRepairTrackedSymbolsAsync(CancellationToken ct = default)
        {
            var symbols =
                await _symbolsRepository.GetSymbolsForBackfillScanAsync(ct);

            foreach (var symbol in symbols)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (symbol.NeedsBackfill)
                    {
                        await BackfillSymbolAsync(symbol.Id, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Backfill scan failed for {Ticker}",
                        symbol.Ticker);
                }
            }
        }

        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                try
                {
                    return await operation(ct);
                }
                catch (HttpRequestException ex) when (
                    ex.StatusCode == HttpStatusCode.Forbidden ||
                    ex.StatusCode == HttpStatusCode.BadRequest ||
                    ex.StatusCode == HttpStatusCode.Unauthorized ||
                    ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(
                        ex,
                        "Backfill request failed with permanent HTTP status {StatusCode}. Not retrying.",
                        ex.StatusCode);

                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt == _options.MaxRetries)
                        break;

                    _logger.LogWarning(
                        ex,
                        "Backfill request failed on attempt {Attempt}/{MaxRetries}. Retrying after delay.",
                        attempt,
                        _options.MaxRetries);

                    await Task.Delay(_options.RetryDelay, ct);
                }
            }

            throw lastException ??
                  new InvalidOperationException("Backfill request failed.");
        }
    }
}