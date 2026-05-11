using StockTraderBackend.Holdings;
using StockTraderBackend.Users;
using StockTraderBackend.Trades;

namespace StockTraderBackend.Portfolios;

public class Portfolio
{
    public Guid portfolioId { get; set; }

    public Guid userId { get; set; }
    public User user { get; set; } = null!;

    public string name { get; set; } = "Default";
    public decimal cashBalance { get; set; } = 1000000m;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;

    public ICollection<Trade> trades { get; set; } = new List<Trade>();
    public ICollection<Holding> holdings { get; set; } = new List<Holding>();

    // EF needs a parameterless constructor (can be private)
    private Portfolio() { }

    public Portfolio(User user, string? name = null)
    {
        this.portfolioId = Guid.NewGuid();
        this.user = user;
        this.userId = user.userId;

        this.cashBalance = 1000000m;
        this.createdAt = DateTime.UtcNow;

        this.name = string.IsNullOrWhiteSpace(name) ? "Default" : name;
    }
}