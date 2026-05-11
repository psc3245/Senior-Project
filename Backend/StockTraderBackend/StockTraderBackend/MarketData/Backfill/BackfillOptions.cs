namespace StockTraderBackend.MarketData.Backfill
{
    public sealed class BackfillOptions
    {
        public TimeSpan RequestDelay { get; set; } = TimeSpan.FromSeconds(12);

        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(60);

        public int MaxRetries { get; set; } = 3;

        public int BackfillYears { get; set; } = 2;
        public bool EnableRunner { get; set; } = false;

    }
}