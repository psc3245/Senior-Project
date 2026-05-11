namespace StockTraderBackend.Assets.PriceBar.DTOs
{
    public sealed record PriceBarDto(
        DateOnly T,   // YYYY-MM-DD
        decimal O,
        decimal H,
        decimal L,
        decimal C,
        decimal V
    );
}
