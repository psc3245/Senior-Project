using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockTraderBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStockSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSplitDetectedUtc",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSplitScanUtc",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stock_splits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SplitFrom = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    SplitTo = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    SplitFactor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    MassiveSplitId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_splits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_splits_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_splits_MassiveSplitId",
                table: "stock_splits",
                column: "MassiveSplitId",
                unique: true,
                filter: "\"MassiveSplitId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_splits_SymbolId_EffectiveDate",
                table: "stock_splits",
                columns: new[] { "SymbolId", "EffectiveDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_splits");

            migrationBuilder.DropColumn(
                name: "LastSplitDetectedUtc",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "LastSplitScanUtc",
                table: "symbols");
        }
    }
}
