using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.MarketData.StockSplits.DTOs;
using StockTraderBackend.MarketData.StockSplits.Service;

namespace StockTraderBackend.MarketData.StockSplits
{
    /// <summary>
    /// Endpoints for inspecting and managing tracked stock split events.
    /// </summary>
    /// <remarks>
    /// Provides read and limited administrative access to stock split metadata stored in the local database.
    /// 
    /// Current behavior:
    /// - Split events are detected from Massive corporate actions data.
    /// - New split events mark tracked symbols for historical backfill.
    /// - Price bars are not directly modified by these endpoints.
    /// 
    /// Notes:
    /// - Stored split events act as a local audit trail and deduplication mechanism.
    /// - Deleting a split event removes the local record only; it does not revert any already-imported
    ///   vendor-adjusted price data.
    /// </remarks>
    [ApiController]
    [Route("api/market/splits")]
    [Produces("application/json")]
    public sealed class StockSplitsController : ControllerBase
    {
        private readonly IStockSplitDetectionService _stockSplitDetectionService;

        public StockSplitsController(IStockSplitDetectionService stockSplitDetectionService)
        {
            _stockSplitDetectionService = stockSplitDetectionService;
        }

        /// <summary>
        /// Gets all locally stored stock split events for a ticker.
        /// </summary>
        /// <param name="ticker">Ticker symbol to look up.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A list of stock split events for the requested ticker, ordered from newest to oldest.
        /// </returns>
        [HttpGet("{ticker}")]
        [ProducesResponseType(typeof(List<StockSplitDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StockSplitDto>>> GetSplitsByTicker(
            string ticker,
            CancellationToken ct)
        {
            var splits = await _stockSplitDetectionService.GetSplitsForTickerAsync(ticker, ct);

            var result = splits
                .Select(x => new StockSplitDto(
                    x.Id,
                    x.Symbol.Ticker,
                    x.EffectiveDate,
                    x.SplitFrom,
                    x.SplitTo,
                    x.SplitFactor,
                    x.MassiveSplitId,
                    x.DetectedAtUtc))
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Scans Massive for recently completed stock splits and stores any new tracked events.
        /// </summary>
        /// <param name="lookbackDays">
        /// Number of trailing days to scan. Defaults to 14.
        /// Only split events with execution dates before today are eligible.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// The number of newly detected split events that were stored locally.
        /// </returns>
        [HttpPost("scan")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> ScanRecentSplits(
            [FromQuery] int lookbackDays = 14,
            CancellationToken ct = default)
        {
            var newSplitCount = await _stockSplitDetectionService.ScanRecentSplitsAsync(lookbackDays, ct);

            return Ok(new
            {
                message = "Split scan completed.",
                newSplitCount
            });
        }

        /// <summary>
        /// Deletes a locally stored stock split record.
        /// </summary>
        /// <param name="splitId">The local stock split record identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <c>204 No Content</c> if the split record was deleted; otherwise <c>404 Not Found</c>.
        /// </returns>
        [HttpDelete("{splitId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteSplit(
            int splitId,
            CancellationToken ct)
        {
            var deleted = await _stockSplitDetectionService.DeleteSplitAsync(splitId, ct);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
