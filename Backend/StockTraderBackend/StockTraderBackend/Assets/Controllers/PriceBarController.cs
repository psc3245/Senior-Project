namespace StockTraderBackend.Assets.Controllers
{
    using global::StockTraderBackend.Assets.PriceBar.DTOs;
    using global::StockTraderBackend.Assets.PriceBar.Services;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Market data endpoints for retrieving historical price bars.
    /// </summary>
    /// <remarks>
    /// Provides read-only access to historical OHLCV price data for supported symbols.
    /// 
    /// Current limitations:
    /// - Only daily timeframes are supported (tf=1d).
    /// - Data is end-of-day (EOD) only; no real-time or intraday bars.
    /// - All data is sourced from the local database; no live provider calls are made.
    /// 
    /// Notes:
    /// - Returned price bars are sorted in ascending chronological order (oldest → newest),
    ///   which is suitable for frontend charting libraries.
    /// - Date filters are inclusive of the <c>from</c> date and exclusive of the <c>to</c> date.
    /// </remarks>
    [ApiController]
    [Route("api/market")]
    [Produces("application/json")]
    public class PriceBarsController : ControllerBase
    {
        private readonly IPriceBarsService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriceBarsController"/>.
        /// </summary>
        /// <param name="service">Service responsible for validating requests and retrieving price bar data.</param>
        public PriceBarsController(IPriceBarsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieve historical price bars for a given symbol.
        /// </summary>
        /// <remarks>
        /// Returns daily OHLCV price bars for the specified symbol.
        /// 
        /// By default:
        /// - The timeframe is daily (<c>tf=1d</c>).
        /// - The most recent 365 bars are returned.
        /// 
        /// Optional query parameters can be used to:
        /// - Restrict the date range using <c>from</c> and <c>to</c>.
        /// - Limit the number of returned bars.
        /// 
        /// If the requested symbol does not exist or the timeframe is unsupported,
        /// an appropriate error response is returned.
        /// </remarks>
        /// <param name="symbol">Ticker symbol (e.g., AAPL, MSFT).</param>
        /// <param name="tf">Timeframe of the price bars. Only <c>1d</c> is supported.</param>
        /// <param name="from">Optional start date (inclusive) for returned bars.</param>
        /// <param name="to">Optional end date (exclusive) for returned bars.</param>
        /// <param name="limit">Maximum number of bars to return (default: 365).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>Historical daily price bars for the requested symbol.</returns>
        /// <response code="200">Price bars retrieved successfully.</response>
        /// <response code="400">Invalid request parameters or unsupported timeframe.</response>
        /// <response code="404">Requested symbol was not found.</response>
        [HttpGet("bars", Name = "GetPriceBars")]
        [ProducesResponseType(typeof(PriceBarsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPriceBars(
            [FromQuery] string symbol,
            [FromQuery] string tf = "1d",
            [FromQuery] DateOnly? from = null,
            [FromQuery] DateOnly? to = null,
            [FromQuery] int limit = 365,
            CancellationToken ct = default)
        {
            var resp = await _service.GetDailyBarsAsync(symbol, tf, from, to, limit, ct);
            return Ok(resp);
        }
    }
}
