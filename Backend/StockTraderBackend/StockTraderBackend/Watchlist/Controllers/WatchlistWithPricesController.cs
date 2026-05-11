using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Watchlist.DTOs;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Watchlist.Controller;

/// <summary>
/// Provides endpoints for watchlists with current stock prices.
/// </summary>
[ApiController]
[Route("api/watchlists/{watchlistId:guid}/prices")]
public sealed class WatchlistWithPricesController : ControllerBase
{
    private readonly IWatchlistWithPricesService _watchlistWithPricesService;

    /// <summary>
    /// Creates a new watchlist with prices controller.
    /// </summary>
    /// <param name="watchlistWithPricesService">
    /// Service used to load watchlists with current stock prices.
    /// </param>
    public WatchlistWithPricesController(
        IWatchlistWithPricesService watchlistWithPricesService)
    {
        _watchlistWithPricesService = watchlistWithPricesService;
    }

    /// <summary>
    /// Gets a watchlist with current prices for each stock.
    /// </summary>
    /// <param name="watchlistId">The ID of the watchlist.</param>
    /// <param name="userId">The ID of the user who owns the watchlist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The watchlist with current prices for each stock.</returns>
    /// <response code="200">Returns the watchlist with current stock prices.</response>
    /// <response code="403">The user does not own this watchlist.</response>
    /// <response code="404">The watchlist or a stock price was not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(WatchlistWithPricesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchlistWithPricesDto>> GetWatchlistWithPrices(
        [FromRoute] Guid watchlistId,
        [FromQuery] Guid userId,
        CancellationToken ct = default)
    {
        var response = await _watchlistWithPricesService.GetWatchlistWithPricesAsync(
            userId,
            watchlistId,
            ct);

        return Ok(response);
    }
}