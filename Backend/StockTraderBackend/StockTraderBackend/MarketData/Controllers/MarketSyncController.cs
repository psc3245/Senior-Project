using Microsoft.AspNetCore.Mvc;

namespace StockTraderBackend.MarketData.Controllers
{
    /// <summary>
    /// Endpoints for manually triggering market data synchronization jobs.
    /// </summary>
    /// <remarks>
    /// Provides internal endpoints for running stock price synchronization against the
    /// configured market data provider.
    /// 
    /// Supported functionality includes:
    /// - Running a grouped daily sync for a specific trading date
    ///
    /// These endpoints are intended for development, testing, and operational use while
    /// validating the market data ingestion pipeline.
    /// </remarks>
    [ApiController]
    [Route("api/market-sync")]
    [Produces("application/json")]
    public class MarketSyncController : ControllerBase
    {
        private readonly DailyGroupedSyncRunner _dailyGroupedSyncRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketSyncController"/>.
        /// </summary>
        /// <param name="dailyGroupedSyncRunner">
        /// Service used to execute grouped daily market data synchronization.
        /// </param>
        public MarketSyncController(DailyGroupedSyncRunner dailyGroupedSyncRunner)
        {
            _dailyGroupedSyncRunner = dailyGroupedSyncRunner;
        }

        /// <summary>
        /// Run a grouped daily stock sync for a specific trading date.
        /// </summary>
        /// <remarks>
        /// Calls the grouped daily aggregates endpoint for the supplied trading date,
        /// filters returned results to symbols already tracked in the system, and upserts
        /// matching daily price bars into the database.
        /// 
        /// This endpoint is primarily intended for manual testing and operational recovery,
        /// such as re-running a missed daily sync.
        /// </remarks>
        /// <param name="date">
        /// The trading date to sync in yyyy-MM-dd format.
        /// </param>
        /// <param name="ct">
        /// Cancellation token for the request.
        /// </param>
        /// <returns>A confirmation that the sync completed successfully.</returns>
        /// <response code="200">The grouped daily sync completed successfully.</response>
        /// <response code="400">The provided date was invalid.</response>
        /// <response code="500">The grouped daily sync failed.</response>
        [HttpPost("daily/{date}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> SyncDaily(
            [FromRoute] DateOnly date,
            CancellationToken ct)
        {
            await _dailyGroupedSyncRunner.RunForDateAsync(date, ct);

            return Ok(new
            {
                message = "Grouped daily sync completed successfully.",
                date
            });
        }
    }
}
