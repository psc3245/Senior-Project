using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Watchlist.DTOs;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Watchlist.Controllers;

/// <summary>
/// Endpoints for managing stocks within watchlists.
/// </summary>
/// <remarks>
/// Provides functionality for adding, removing, and retrieving stocks in a specific watchlist.
/// 
/// Supported operations include:
/// - Adding stocks to a watchlist
/// - Removing stocks from a watchlist
/// - Retrieving all stocks in a watchlist
/// </remarks>
[ApiController]
[Route("api/watchlists/{watchlistId}/stocks")]
[Produces("application/json")]
public class WatchlistStockController : ControllerBase
{
    private readonly IWatchlistStockService _stockService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistStockController"/>.
    /// </summary>
    /// <param name="stockService">Service used to manage watchlist stock operations.</param>
    public WatchlistStockController(IWatchlistStockService stockService)
    {
        _stockService = stockService;
    }

    /// <summary>
    /// Get all stocks in a watchlist.
    /// </summary>
    /// <remarks>
    /// Retrieves all stocks in the specified watchlist, ordered by position.
    /// Includes full symbol information for each stock.
    /// </remarks>
    /// <param name="watchlistId">The ID of the watchlist.</param>
    /// <returns>A list of stocks in the watchlist.</returns>
    /// <response code="200">Stocks retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<WatchlistStock>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStocks(Guid watchlistId)
    {
        var stocks = await _stockService.GetWatchlistStocks(watchlistId);
        return Ok(stocks);
    }

    /// <summary>
    /// Add a stock to a watchlist.
    /// </summary>
    /// <remarks>
    /// Adds the specified stock symbol to the watchlist.
    /// The stock is automatically positioned at the end of the list.
    /// Duplicate stocks in the same watchlist are not allowed.
    /// You may provide either a SymbolId or a Ticker.
    /// </remarks>
    /// <param name="watchlistId">The ID of the watchlist.</param>
    /// <param name="request">The request containing either the symbol ID or ticker to add.</param>
    /// <returns>The newly added watchlist stock entry.</returns>
    /// <response code="200">Stock added successfully.</response>
    /// <response code="400">Stock already exists in watchlist or request was invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WatchlistStock), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddStock(Guid watchlistId, [FromBody] AddStockRequest request)
    {
        var stock = await _stockService.AddStockToWatchlist(watchlistId, request);
        return Ok(stock);
    }

    /// <summary>
    /// Remove a stock from a watchlist.
    /// </summary>
    /// <remarks>
    /// Permanently removes the specified stock from the watchlist.
    /// </remarks>
    /// <param name="watchlistId">The ID of the watchlist.</param>
    /// <param name="symbolId">The ID of the symbol to remove.</param>
    /// <returns>No content on successful removal.</returns>
    /// <response code="204">Stock removed successfully.</response>
    /// <response code="404">Stock not found in watchlist.</response>
    [HttpDelete("{symbolId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStock(Guid watchlistId, int symbolId)
    {
        await _stockService.RemoveStockFromWatchlist(watchlistId, symbolId);
        return NoContent();
    }
}