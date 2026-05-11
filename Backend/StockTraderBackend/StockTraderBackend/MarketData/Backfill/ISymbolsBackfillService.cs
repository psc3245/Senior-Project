namespace StockTraderBackend.MarketData.Backfill
{
    public interface ISymbolBackfillService
    {
        Task BackfillSymbolAsync(int symbolId, CancellationToken cancellationToken = default);
        Task ScanAndRepairTrackedSymbolsAsync(CancellationToken cancellationToken = default);
    }
}