using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StockTraderBackend.MarketData.Backfill
{
    public sealed class SymbolBackfillRunner : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SymbolBackfillRunner> _logger;

        public SymbolBackfillRunner(
            IServiceScopeFactory scopeFactory,
            ILogger<SymbolBackfillRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var backfillService = scope.ServiceProvider.GetRequiredService<ISymbolBackfillService>();

                    await backfillService.ScanAndRepairTrackedSymbolsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SymbolBackfillRunner failed");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}