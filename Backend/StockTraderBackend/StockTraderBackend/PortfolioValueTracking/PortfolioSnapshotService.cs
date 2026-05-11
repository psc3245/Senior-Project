namespace StockTraderBackend.PortfolioValueTracking;

public sealed class PortfolioSnapshotService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioSnapshotService> _logger;

    public PortfolioSnapshotService(
        IServiceScopeFactory scopeFactory,
        ILogger<PortfolioSnapshotService> logger)
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

            var nextSnapshotTime = GetNextRunTime(nowEastern);
            var delay = nextSnapshotTime.ToUniversalTime() - nowUtc;

            _logger.LogInformation(
                "Next portfolio snapshot scheduled for {NextSnapshotEastern} Eastern / {NextSnapshotUtc} UTC",
                nextSnapshotTime,
                nextSnapshotTime.ToUniversalTime());

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<PortfolioSnapshotRunner>();

                var snapshotUtc = DateTimeOffset.UtcNow;
                var snapshotEastern = TimeZoneInfo.ConvertTime(snapshotUtc, easternTimeZone);

                if (snapshotEastern.DayOfWeek == DayOfWeek.Sunday)
                {
                    _logger.LogInformation("Skipping portfolio snapshot because today is Sunday.");
                    continue;
                }

                var tradingDate = GetPreviousTradingDay(snapshotEastern.Date);

                _logger.LogInformation(
                    "Taking portfolio snapshot on {SnapshotDate} Eastern for trading date {TradingDate}",
                    snapshotEastern.Date,
                    tradingDate);

                await runner.UpsertAllPortfolioSnapshots(tradingDate, stoppingToken);

                _logger.LogInformation("Portfolio snapshot completed for {TradingDate}", tradingDate);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portfolio snapshot failed.");
            }
        }
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
            candidate = candidate.AddDays(1);

        while (candidate.DayOfWeek == DayOfWeek.Sunday)
            candidate = candidate.AddDays(1);

        return new DateTimeOffset(
            candidate.Year,
            candidate.Month,
            candidate.Day,
            17,
            30,
            0,
            candidate.Offset);
    }

    private static string GetEasternTimeZoneId() =>
        OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York";
}