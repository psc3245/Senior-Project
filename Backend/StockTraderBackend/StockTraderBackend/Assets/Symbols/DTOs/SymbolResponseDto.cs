namespace StockTraderBackend.Assets.Symbols.DTOs
{
    public class SymbolResponseDto
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Exchange { get; set; } = null!;
        public AssetTypes AssetType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
