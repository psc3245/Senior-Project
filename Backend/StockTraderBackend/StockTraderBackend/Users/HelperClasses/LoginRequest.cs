namespace StockTraderBackend.Users.HelperClasses;

public class LoginRequest
{
    public string? username { get; set; }
    public string? email { get; set; } 
    public string password { get; set; }
}