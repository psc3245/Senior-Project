namespace StockTraderBackend.Watchlist.DTOs;

public class CreateWatchlistRequest
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
}