namespace StockTraderBackend.MarketData.Massive
{
    public sealed class MassiveOptions
    {
        public const string SectionName = "Massive";

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.massive.com";
    }
}
