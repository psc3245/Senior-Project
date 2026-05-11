using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockTraderBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Image = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SourceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "symbols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ticker = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Default"),
                    password = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.userId);
                });

            migrationBuilder.CreateTable(
                name: "price_bars",
                columns: table => new
                {
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    Timeframe = table.Column<string>(type: "text", nullable: false),
                    Ts = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_bars", x => new { x.SymbolId, x.Timeframe, x.Ts });
                    table.ForeignKey(
                        name: "FK_price_bars_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorite_articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FavoritedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_favorite_articles_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_favorite_articles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                columns: table => new
                {
                    portfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Default"),
                    cashBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 1000000m),
                    createdAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolios", x => x.portfolioId);
                    table.ForeignKey(
                        name: "FK_portfolios_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "holdings",
                columns: table => new
                {
                    holdingId = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    avgBuyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holdings", x => x.holdingId);
                    table.ForeignKey(
                        name: "FK_holdings_portfolios_portfolioId",
                        column: x => x.portfolioId,
                        principalTable: "portfolios",
                        principalColumn: "portfolioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    tradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tradeType = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    priceAtTrade = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    totalValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    executedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.tradeId);
                    table.ForeignKey(
                        name: "FK_trades_portfolios_portfolioId",
                        column: x => x.portfolioId,
                        principalTable: "portfolios",
                        principalColumn: "portfolioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_articles_Url",
                table: "articles",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_favorite_articles_ArticleId",
                table: "favorite_articles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_articles_UserId",
                table: "favorite_articles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_articles_UserId_ArticleId",
                table: "favorite_articles",
                columns: new[] { "UserId", "ArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holdings_portfolioId",
                table: "holdings",
                column: "portfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_holdings_ticker",
                table: "holdings",
                column: "ticker");

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_userId",
                table: "portfolios",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_price_bars_SymbolId_Ts",
                table: "price_bars",
                columns: new[] { "SymbolId", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_symbols_Ticker",
                table: "symbols",
                column: "Ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trades_portfolioId",
                table: "trades",
                column: "portfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_trades_ticker",
                table: "trades",
                column: "ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorite_articles");

            migrationBuilder.DropTable(
                name: "holdings");

            migrationBuilder.DropTable(
                name: "price_bars");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "symbols");

            migrationBuilder.DropTable(
                name: "portfolios");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
