using System.Text.Json.Serialization;
using StockTraderBackend.Portfolios;

namespace StockTraderBackend.Users;

public class User
{
    public  Guid userId {get; set;}
    
    public string username { get; set; }
    
    public string password { get; set; }
    
    public string email { get; set; }

    [JsonIgnore]
    public ICollection<Portfolio> portfolios { get; set; } = new List<Portfolio>();

    public User(string username, string password, string email)
    {
        this.userId = Guid.NewGuid();
        this.username = username;
        this.password = password;
        this.email = email;
        this.portfolios.Add(new Portfolio(this));
    }
    
    
}