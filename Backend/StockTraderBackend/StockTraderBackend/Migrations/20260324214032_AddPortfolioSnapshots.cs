using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTraderBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioSnapshotHoldings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "text", nullable: false),
                    Shares = table.Column<decimal>(type: "numeric", nullable: false),
                    PriceAtSnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshotHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioSnapshotHoldings_PortfolioSnapshots_PortfolioSnaps~",
                        column: x => x.PortfolioSnapshotId,
                        principalTable: "PortfolioSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshotHoldings_PortfolioSnapshotId",
                table: "PortfolioSnapshotHoldings",
                column: "PortfolioSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioSnapshotHoldings");

            migrationBuilder.DropTable(
                name: "PortfolioSnapshots");
        }
    }
}
