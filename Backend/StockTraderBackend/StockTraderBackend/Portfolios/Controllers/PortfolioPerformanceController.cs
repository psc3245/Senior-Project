using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.Portfolios.Services;

namespace StockTraderBackend.Portfolios.Controller;

/// <summary>
/// Provides endpoints for viewing current portfolio performance.
/// </summary>
[ApiController]
[Route("api/portfolios/{portfolioId:guid}/performance")]
public sealed class PortfolioPerformanceController : ControllerBase
{
    private readonly IPortfolioPerformanceService _portfolioPerformanceService;

    /// <summary>
    /// Creates a new portfolio performance controller.
    /// </summary>
    /// <param name="portfolioPerformanceService">
    /// Service used to calculate portfolio performance summaries.
    /// </param>
    public PortfolioPerformanceController(
        IPortfolioPerformanceService portfolioPerformanceService)
    {
        _portfolioPerformanceService = portfolioPerformanceService;
    }

    /// <summary>
    /// Gets a current performance summary for a portfolio.
    /// </summary>
    /// <remarks>
    /// This endpoint calculates the current value of the portfolio using the latest available stock prices.
    /// It includes cash balance, holdings value, total portfolio value, total cost basis,
    /// unrealized gain or loss, gain/loss percentage, top gainer, top loser, and each position's performance.
    ///
    /// This is different from portfolio snapshots. Snapshots show historical portfolio value over time,
    /// while this endpoint shows the portfolio's current unrealized performance compared to average buy price.
    /// </remarks>
    /// <param name="portfolioId">The ID of the portfolio to summarize.</param>
    /// <param name="userId">The ID of the user who owns the portfolio.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current performance summary for the portfolio.</returns>
    /// <response code="200">Returns the portfolio performance summary.</response>
    /// <response code="403">The user does not own this portfolio.</response>
    /// <response code="404">The portfolio was not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioPerformanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortfolioPerformanceSummaryDto>> GetPortfolioPerformanceSummary(
        [FromRoute] Guid portfolioId,
        [FromQuery] Guid userId,
        CancellationToken ct = default)
    {
        var summary = await _portfolioPerformanceService
            .GetPortfolioPerformanceSummaryAsync(userId, portfolioId, ct);

        return Ok(summary);
    }
}