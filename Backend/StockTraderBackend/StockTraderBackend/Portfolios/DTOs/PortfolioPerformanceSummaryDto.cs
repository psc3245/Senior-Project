namespace StockTraderBackend.Portfolios.DTOs;

public sealed class PortfolioPerformanceSummaryDto
{
    public Guid PortfolioId { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal CashBalance { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal TotalPortfolioValue { get; set; }

    public decimal TotalCostBasis { get; set; }
    public decimal TotalGainLoss { get; set; }
    public decimal TotalGainLossPercent { get; set; }

    public PortfolioPositionPerformanceDto? TopGainer { get; set; }
    public PortfolioPositionPerformanceDto? TopLoser { get; set; }

    public List<PortfolioPositionPerformanceDto> Positions { get; set; } = [];
}