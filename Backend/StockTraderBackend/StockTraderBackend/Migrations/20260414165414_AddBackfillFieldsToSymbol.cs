using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockTraderBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBackfillFieldsToSymbol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastBackfillAttemptUtc",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBackfillSuccessUtc",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsBackfill",
                table: "symbols",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastBackfillAttemptUtc",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "LastBackfillSuccessUtc",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "NeedsBackfill",
                table: "symbols");
        }
    }
}
