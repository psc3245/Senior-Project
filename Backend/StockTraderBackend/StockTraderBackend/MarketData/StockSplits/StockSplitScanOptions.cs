namespace StockTraderBackend.MarketData.StockSplits
{
    public sealed class StockSplitScanOptions
    {
        public const string SectionName = "StockSplitScan";

        public bool Enabled { get; set; } = false;

        // How many past days to scan for split events.
        public int LookbackDays { get; set; } = 14;

        // How often the hosted service should wake up and run.
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(24);

        // Optional delay before first run after startup.
        public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(1);

        // If true, run once immediately after startup delay.
        public bool RunOnStartup { get; set; } = true;
    }
}
