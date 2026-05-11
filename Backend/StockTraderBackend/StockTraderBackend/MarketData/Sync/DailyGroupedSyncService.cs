using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StockTraderBackend.MarketData.Sync
{
    public sealed class DailyGroupedSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyGroupedSyncService> _logger;

        public DailyGroupedSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<DailyGroupedSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(GetEasternTimeZoneId());

            while (!stoppingToken.IsCancellationRequested)
            {
                var nowUtc = DateTimeOffset.UtcNow;
                var nowEastern = TimeZoneInfo.ConvertTime(nowUtc, easternTimeZone);

                var nextRunEastern = GetNextRunTime(nowEastern);
                var delay = nextRunEastern.ToUniversalTime() - nowUtc;

                _logger.LogInformation(
                    "Next market sync scheduled for {NextRunEastern} Eastern / {NextRunUtc} UTC",
                    nextRunEastern,
                    nextRunEastern.ToUniversalTime());

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<DailyGroupedSyncRunner>();

                    var runNowUtc = DateTimeOffset.UtcNow;
                    var runNowEastern = TimeZoneInfo.ConvertTime(runNowUtc, easternTimeZone);

                    // Sunday has no new previous trading day to fetch that wasn't already
                    // available on Saturday.
                    if (runNowEastern.DayOfWeek == DayOfWeek.Sunday)
                    {
                        _logger.LogInformation("Skipping sync because today is Sunday.");
                        continue;
                    }

                    var tradingDate = GetPreviousTradingDay(runNowEastern.Date);

                    _logger.LogInformation(
                        "Running grouped daily sync on {RunDate} Eastern for trading date {TradingDate}",
                        runNowEastern.Date,
                        tradingDate);

                    await runner.RunForDateAsync(tradingDate, stoppingToken);

                    _logger.LogInformation("Market sync completed for {TradingDate}", tradingDate);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Market sync failed.");
                }
            }
        }

        private static DateTimeOffset GetNextRunTime(DateTimeOffset nowEastern)
        {
            var candidate = new DateTimeOffset(
                nowEastern.Year,
                nowEastern.Month,
                nowEastern.Day,
                17,
                30,
                0,
                nowEastern.Offset);

            if (nowEastern >= candidate)
            {
                candidate = candidate.AddDays(1);
            }

            // Skip Sunday only. Saturday is needed so Friday's data can be fetched.
            while (candidate.DayOfWeek == DayOfWeek.Sunday)
            {
                candidate = candidate.AddDays(1);
            }

            return new DateTimeOffset(
                candidate.Year,
                candidate.Month,
                candidate.Day,
                17,
                30,
                0,
                candidate.Offset);
        }

        private static DateOnly GetPreviousTradingDay(DateTime date)
        {
            var candidate = DateOnly.FromDateTime(date.AddDays(-1));

            while (candidate.DayOfWeek == DayOfWeek.Saturday ||
                   candidate.DayOfWeek == DayOfWeek.Sunday)
            {
                candidate = candidate.AddDays(-1);
            }

            return candidate;
        }

        private static string GetEasternTimeZoneId() =>
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York";
    }
}