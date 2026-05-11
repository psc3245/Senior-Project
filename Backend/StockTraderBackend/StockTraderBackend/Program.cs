using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockTraderBackend.Assets.PriceBar;
using StockTraderBackend.Assets.PriceBar.Services;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.Assets.Symbols.Service;
using StockTraderBackend.Chat;
using StockTraderBackend.Chat.AI;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Chat.Tools;
using StockTraderBackend.Chat.Tools.Level_1;
using StockTraderBackend.Chat.Tools.Level_2;
using StockTraderBackend.Chat.Tools.Level_3;
using StockTraderBackend.Data;
using StockTraderBackend.MarketData.Backfill;
using StockTraderBackend.MarketData.Calendar;
using StockTraderBackend.MarketData.Massive;
using StockTraderBackend.MarketData.Splits;
using StockTraderBackend.MarketData.StockSplits;
using StockTraderBackend.MarketData.StockSplits.Repository;
using StockTraderBackend.MarketData.StockSplits.Service;
using StockTraderBackend.MarketData.Sync;
using StockTraderBackend.News.Clients;
using StockTraderBackend.News.Repositories;
using StockTraderBackend.News.Services;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Portfolios.Performance;
using StockTraderBackend.Portfolios.Services;
using StockTraderBackend.PortfolioValueTracking;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Trades.Repositories;
using StockTraderBackend.Trades.Services;
using StockTraderBackend.Users;
using StockTraderBackend.Watchlist.Repository;
using StockTraderBackend.Watchlist.Service;
using StockTraderBackend.Watchlists.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("null", "http://localhost:3000", "http://localhost:5500", "http://localhost:8080", "http://127.0.0.1:5500", "http://coms-4020-031.class.las.iastate.edu:8443", "http://10.90.75.178:8443")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<IPriceBarsService, PriceBarsService>();
builder.Services.AddScoped<ISymbolsRepository, SymbolsRepository>();
builder.Services.AddScoped<IPriceBarsRepository, PriceBarsRepository>();


builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PortfolioRepository>();
builder.Services.AddScoped<TradeRepository>();

builder.Services.AddScoped<IPortfolioService, PortfolioService>(); 
builder.Services.AddScoped<IStockAPIService, StockAPIService>();
//trade services
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<ITradeEstimateService, TradeEstimateService>();
builder.Services.AddScoped<UserService>();
//news services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IGNewsClient, GNewsClient>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IFavoriteArticleService, FavoriteArticleService>();
//watchlist services
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IWatchlistStockService, WatchlistStockService>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<IWatchlistWithPricesService, WatchlistWithPricesService>();
// Chat History and AI Client
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IAIClient, AIClient>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
}).AddHubOptions<ChatHub>(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection(AIOptions.SectionName));

//massive/stock data ingenstion services
builder.Services.Configure<MassiveOptions>(
    builder.Configuration.GetSection(MassiveOptions.SectionName));
builder.Services.AddHttpClient<IMassiveMarketDataClient, MassiveMarketDataClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MassiveOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
});
//Sync services
builder.Services.AddHostedService<DailyGroupedSyncService>();
builder.Services.AddScoped<DailyGroupedSyncRunner>();
//backfill services
builder.Services.AddScoped<ISymbolBackfillService, SymbolBackfillService>();
builder.Services.AddSingleton<IMarketCalendarService, DefaultMarketCalendarService>();
builder.Services.Configure<BackfillOptions>(
    builder.Configuration.GetSection("Backfill"));
var backfillOptions = builder.Configuration
    .GetSection("Backfill")
    .Get<BackfillOptions>();
if (backfillOptions?.EnableRunner == true)
{
    builder.Services.AddHostedService<SymbolBackfillRunner>();
}
//Split Services
builder.Services.AddScoped<IStockSplitsRepository, StockSplitsRepository>();
builder.Services.AddHttpClient<IMassiveCorporateActionsClient, MassiveCorporateActionsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Massive:BaseUrl"]!);
});
builder.Services.AddScoped<IStockSplitDetectionService, StockSplitDetectionService>();
builder.Services
    .AddOptions<StockSplitScanOptions>()
    .Bind(builder.Configuration.GetSection(StockSplitScanOptions.SectionName));
builder.Services.AddHostedService<StockSplitScanHostedService>();
//portfolio snapshot services
builder.Services.AddScoped<PortfolioSnapshotRepository>();
builder.Services.AddScoped<PortfolioSnapshotRunner>();
builder.Services.AddHostedService<PortfolioSnapshotService>();
builder.Services.AddScoped<IPortfolioSnapshotQueryService, PortfolioSnapshotQueryService>();
builder.Services.AddScoped<IPortfolioPerformanceService, PortfolioPerformanceService>();
//Symbol Services
builder.Services.AddScoped<ISymbolsRepository, SymbolsRepository>();
builder.Services.AddScoped<ISymbolsService, SymbolsService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
//LLM Tools services
builder.Services.AddScoped<ILlmTool, GetSymbolTool>();
builder.Services.AddScoped<ILlmTool, ListSymbolsTool>();
builder.Services.AddScoped<ILlmTool, GetUserPortfolioTool>();
builder.Services.AddScoped<ILlmTool, GetRandomSymbolsTool>();
builder.Services.AddScoped<ILlmTool, GetCurrentStockPriceTool>();
builder.Services.AddScoped<ILlmTool, GetDailyPriceBarsTool>();
builder.Services.AddScoped<ILlmTool, SearchArticlesTool>();
builder.Services.AddScoped<ILlmTool, GetFavoriteArticlesTool>();
builder.Services.AddScoped<ILlmTool, GetUserWatchlistsTool>();
builder.Services.AddScoped<ILlmTool, GetWatchlistStocksTool>();
builder.Services.AddScoped<ILlmTool, GetPortfolioSnapshotsTool>();
builder.Services.AddScoped<ILlmTool, GetStockSplitsTool>();
builder.Services.AddScoped<ILlmTool, GetPortfolioPerformanceSummaryTool>();
builder.Services.AddScoped<ILlmTool, GetWatchlistWithPricesTool>();
builder.Services.AddScoped<ILlmTool, GetPricesForTickersTool>();
builder.Services.AddScoped<ILlmTool, EstimateTradeTool>();
builder.Services.AddScoped<ILlmTool, ExecuteTradeTool>();
builder.Services.AddScoped<LlmToolDispatcher>();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
});

app.UseCors("AllowAll");

app.UseAuthorization();

app.UseMiddleware<StockTraderBackend.Middleware.ApiExceptionMiddleware>();

app.MapControllers();

app.MapHub<ChatHub>("/chat", options =>
{
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(3);
    options.WebSockets.SubProtocolSelector = protocols => string.Empty;
}).RequireCors("AllowAll");

app.Run();