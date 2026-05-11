using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using StockTraderBackend.News.Models;
using System.Diagnostics.Metrics;

namespace StockTraderBackend.Chat.AI;

public class AIOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1-mini";
    public int MaxHistoryMessages { get; set; } = 20;

    public string SystemPrompt { get; set; } =
        "You are a helpful assistant inside a stock trading application. \n" +
        "Be concise, accurate, and practical.\n " +
        "Do not invent account data, prices, holdings, or actions the user has not provided. " +
        "If the user asks for financial guidance, try to be as helpful as possible." +
        "You may use tools to retrieve user portfolio, watchlists, and articles.\n" +
        "If the user asks about their portfolio or account, you MUST call a tool instead of guessing.\n" +
        "Never fabricate financial data.\n" +
        "You cannot perform trades or modify data.\n";
}
