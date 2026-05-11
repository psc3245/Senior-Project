using StockTraderBackend.Users;

namespace StockTraderBackend.Watchlist;

public class Watchlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; }
    public List<WatchlistStock> Stocks { get; set; } = new();

}