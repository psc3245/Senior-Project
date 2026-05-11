using Microsoft.Extensions.Options;
using StockTraderBackend.MarketData.StockSplits.Service;
using StockTraderBackend.MarketData.StockSplits;

namespace StockTraderBackend.MarketData.Splits
{
    public sealed class StockSplitScanHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<StockSplitScanOptions> _options;
        private readonly ILogger<StockSplitScanHostedService> _logger;

        public StockSplitScanHostedService(
            IServiceProvider serviceProvider,
            IOptions<StockSplitScanOptions> options,
            ILogger<StockSplitScanHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = _options.Value;

            if (!options.Enabled)
            {
                _logger.LogInformation("Stock split scan hosted service is disabled.");
                return;
            }

            if (options.StartupDelay > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Stock split scan hosted service delaying startup for {StartupDelay}.",
                    options.StartupDelay);

                await Task.Delay(options.StartupDelay, stoppingToken);
            }

            if (options.RunOnStartup)
            {
                await RunOnceAsync(options.LookbackDays, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Stock split scan hosted service sleeping for {PollInterval}.",
                    options.PollInterval);

                await Task.Delay(options.PollInterval, stoppingToken);

                await RunOnceAsync(options.LookbackDays, stoppingToken);
            }
        }

        private async Task RunOnceAsync(int lookbackDays, CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var splitDetectionService =
                    scope.ServiceProvider.GetRequiredService<IStockSplitDetectionService>();

                var newSplitCount = await splitDetectionService.ScanRecentSplitsAsync(
                    lookbackDays,
                    ct);

                _logger.LogInformation(
                    "Stock split scan completed. New splits detected: {NewSplitCount}.",
                    newSplitCount);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock split scan hosted service failed.");
            }
        }
    }
}