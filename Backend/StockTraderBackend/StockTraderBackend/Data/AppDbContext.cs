using StockTraderBackend.Chat;
using StockTraderBackend.Holdings;
using StockTraderBackend.Trades;
using StockTraderBackend.Users;
using StockTraderBackend.News.Models;
using StockTraderBackend.PortfolioValueTracking;
using StockTraderBackend.Watchlist;

namespace StockTraderBackend.Data
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using Microsoft.EntityFrameworkCore;
    using StockTraderBackend.Assets.PriceBar.Models;
    using StockTraderBackend.Assets.Symbols.Model;
    using StockTraderBackend.MarketData.StockSplits.Models;
    using StockTraderBackend.Portfolios;

    public class AppDbContext : DbContext
    {
        public DbSet<Symbol> Symbols => Set<Symbol>();
        public DbSet<PriceBar> PriceBars => Set<PriceBar>();
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Trade> Trades => Set<Trade>();
        public DbSet<Holding> Holdings => Set<Holding>();
        public DbSet<Article> Articles { get; set; }
        public DbSet<FavoriteArticle> FavoriteArticles { get; set; }
        public DbSet<Watchlist.Watchlist> Watchlists => Set<Watchlist.Watchlist>();
        public DbSet<WatchlistStock> WatchlistStocks => Set<WatchlistStock>();
        
        public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();
        public DbSet<PortfolioSnapshotHolding> PortfolioSnapshotHoldings => Set<PortfolioSnapshotHolding>();
        public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<StockSplit> StockSplits => Set<StockSplit>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Symbol>(e =>
            {
                e.ToTable("symbols");
                e.HasKey(x => x.Id);

                e.Property(x => x.Ticker).IsRequired();
                e.HasIndex(x => x.Ticker).IsUnique();

                e.Property(x => x.Name).IsRequired();
                e.Property(x => x.Exchange).IsRequired();
                e.Property(x => x.AssetType).IsRequired();

                e.Property(x => x.IsActive).HasDefaultValue(true);

                e.Property(x => x.CreatedAt)
                 .HasColumnType("timestamptz")
                 .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<PriceBar>(e =>
            {
                e.ToTable("price_bars");

                e.HasKey(x => new { x.SymbolId, x.Timeframe, x.Ts });

                e.Property(x => x.Timeframe).IsRequired();
                e.Property(x => x.Ts).HasColumnType("timestamptz");

                e.Property(x => x.Open).HasColumnType("numeric(18,6)");
                e.Property(x => x.High).HasColumnType("numeric(18,6)");
                e.Property(x => x.Low).HasColumnType("numeric(18,6)");
                e.Property(x => x.Close).HasColumnType("numeric(18,6)");
                e.Property(p => p.Volume)
                    .HasColumnType("numeric(20,6)");
                e.HasOne(x => x.Symbol)
                 .WithMany(s => s.PriceBars)
                 .HasForeignKey(x => x.SymbolId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.SymbolId, x.Ts });
            });

            modelBuilder.Entity<Portfolio>(e =>
            {
                e.ToTable("portfolios");

                e.HasKey(p => p.portfolioId);

                e.Property(p => p.portfolioId)
                    .ValueGeneratedNever(); // you generate Guid in code

                e.Property(p => p.userId).IsRequired();

                e.Property(p => p.name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue("Default");

                e.Property(p => p.cashBalance)
                    .HasColumnType("numeric(18,2)")
                    .HasDefaultValue(1000000m);

                e.Property(p => p.createdAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                // User 1 -> many Portfolios
                e.HasOne(p => p.user)
                    .WithMany() // if User has ICollection<Portfolio> portfolios, change to .WithMany(u => u.portfolios)
                    .HasForeignKey(p => p.userId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(p => p.userId);
            });
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");

                e.HasKey(u => u.userId);

                e.Property(u => u.userId)
                    .ValueGeneratedNever();

                e.Property(u => u.username)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue("Default");

                e.Property(u => u.email)
                    .IsRequired()
                    .HasMaxLength(256);

                e.Property(u => u.password)
                    .IsRequired()
                    .HasMaxLength(512); // enough for a bcrypt hash

                e.HasMany<Portfolio>(u => u.portfolios)
                    .WithOne(p => p.user)
                    .HasForeignKey(p => p.userId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Trade>(e =>
            {
                e.ToTable("trades");

                e.HasKey(t => t.tradeId);

                e.Property(t => t.tradeId)
                    .ValueGeneratedNever();

                e.Property(t => t.ticker)
                    .IsRequired()
                    .HasMaxLength(10);

                e.Property(t => t.tradeType)
                    .IsRequired()
                    .HasConversion<string>(); // stores "Buy"/"Sell" instead of 0/1

                e.Property(t => t.quantity)
                    .IsRequired()
                    .HasColumnType("numeric(18,8)"); // 8 decimal places for fractional shares

                e.Property(t => t.priceAtTrade)
                    .IsRequired()
                    .HasColumnType("numeric(18,2)");

                e.Property(t => t.totalValue)
                    .IsRequired()
                    .HasColumnType("numeric(18,2)");

                e.Property(t => t.executedAt)
                    .IsRequired()
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                // Trade many -> 1 Portfolio
                e.HasOne(t => t.portfolio)
                    .WithMany(p => p.trades)
                    .HasForeignKey(t => t.portfolioId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(t => t.portfolioId); // useful since you'll query trades by portfolio often
                e.HasIndex(t => t.ticker);      // useful for stock-specific history queries
            });
            modelBuilder.Entity<Holding>(e =>
            {
                e.ToTable("holdings");

                e.HasKey(h => h.holdingId);

                e.Property(h => h.holdingId)
                    .ValueGeneratedNever();

                e.Property(h => h.ticker)
                    .IsRequired()
                    .HasMaxLength(10);

                e.Property(h => h.quantity)
                    .IsRequired()
                    .HasColumnType("numeric(18,8)");

                e.Property(h => h.avgBuyPrice)
                    .IsRequired()
                    .HasColumnType("numeric(18,2)");

                e.HasOne(h => h.portfolio)
                    .WithMany(p => p.holdings)
                    .HasForeignKey(h => h.portfolioId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(h => h.portfolioId);
                e.HasIndex(h => h.ticker);
            });
            modelBuilder.Entity<Article>(e =>
            {
                e.ToTable("articles");

                e.HasKey(a => a.Id);

                e.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                e.Property(a => a.Description)
                    .HasMaxLength(2000);

                e.Property(a => a.Content)
                    .HasMaxLength(10000);

                e.Property(a => a.Url)
                    .IsRequired()
                    .HasMaxLength(2000);

                e.Property(a => a.Image)
                    .HasMaxLength(2000);

                e.Property(a => a.PublishedAt)
                    .HasColumnType("timestamptz");

                e.Property(a => a.SourceName)
                    .HasMaxLength(255);

                e.Property(a => a.SourceUrl)
                    .HasMaxLength(2000);

                e.Property(a => a.CreatedAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                // one stored article per URL
                e.HasIndex(a => a.Url).IsUnique();
            });

            modelBuilder.Entity<FavoriteArticle>(e =>
            {
                e.ToTable("favorite_articles");

                e.HasKey(f => f.Id);

                e.Property(f => f.UserId)
                    .IsRequired();

                e.Property(f => f.ArticleId)
                    .IsRequired();

                e.Property(f => f.FavoritedAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                // one user cannot favorite same article twice
                e.HasIndex(f => new { f.UserId, f.ArticleId }).IsUnique();

                e.HasIndex(f => f.UserId);
                e.HasIndex(f => f.ArticleId);

                // foreign key to users table
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // foreign key to articles table
                e.HasOne<Article>()
                    .WithMany()
                    .HasForeignKey(f => f.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Watchlist.Watchlist>(e =>
            {
                e.ToTable("watchlists");

                e.HasKey(w => w.Id);

                e.Property(w => w.UserId)
                    .IsRequired();

                e.Property(w => w.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(w => w.CreatedAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                e.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(w => w.UserId);
            });

            modelBuilder.Entity<WatchlistStock>(e =>
            {
                e.ToTable("watchlist_stocks");

                e.HasKey(ws => new { ws.WatchlistId, ws.SymbolId });

                e.Property(ws => ws.Position)
                    .IsRequired();

                e.Property(ws => ws.AddedAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                e.HasOne(ws => ws.Watchlist)
                    .WithMany(w => w.Stocks)
                    .HasForeignKey(ws => ws.WatchlistId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ws => ws.Symbol)
                    .WithMany()
                    .HasForeignKey(ws => ws.SymbolId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(ws => ws.WatchlistId);
            });
            
            modelBuilder.Entity<PortfolioSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasMany(e => e.Holdings)
                    .WithOne()
                    .HasForeignKey(h => h.PortfolioSnapshotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PortfolioSnapshotHolding>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
            modelBuilder.Entity<ChatHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasMany(e => e.Messages)
                    .WithOne()
                    .HasForeignKey(m => m.ChatHistoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<StockSplit>(e =>
            {
                e.ToTable("stock_splits");

                e.HasKey(x => x.Id);

                e.Property(x => x.EffectiveDate)
                    .IsRequired();

                e.Property(x => x.SplitFrom)
                    .HasColumnType("numeric(18,6)")
                    .IsRequired();

                e.Property(x => x.SplitTo)
                    .HasColumnType("numeric(18,6)")
                    .IsRequired();

                e.Property(x => x.SplitFactor)
                    .HasColumnType("numeric(18,6)")
                    .IsRequired();

                e.Property(x => x.MassiveSplitId)
                    .HasMaxLength(100);

                e.Property(x => x.DetectedAtUtc)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("now()");

                // prevent duplicate splits
                e.HasIndex(x => new { x.SymbolId, x.EffectiveDate })
                    .IsUnique();

                // optional but helpful if vendor gives stable IDs
                e.HasIndex(x => x.MassiveSplitId)
                    .IsUnique()
                    .HasFilter("\"MassiveSplitId\" IS NOT NULL");

                e.HasOne(x => x.Symbol)
                    .WithMany(s => s.StockSplits)
                    .HasForeignKey(x => x.SymbolId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

}
