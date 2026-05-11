using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.PortfolioValueTracking.DTOs;

namespace StockTraderBackend.PortfolioValueTracking
{
    /// <summary>
    /// Endpoints for manually triggering portfolio value snapshot jobs.
    /// </summary>
    /// <remarks>
    /// Provides internal endpoints for capturing the value of all portfolios
    /// at a specific point in time.
    ///
    /// Supported functionality includes:
    /// - Taking a portfolio value snapshot for a specific trading date
    ///
    /// These endpoints are intended for development, testing, and operational use while
    /// validating the portfolio value tracking pipeline.
    /// </remarks>
    [ApiController]
    [Route("api/portfolio/snapshot")]
    [Produces("application/json")]
    public class PortfolioSnapshotController : ControllerBase
    {
        private readonly PortfolioSnapshotRunner _portfolioSnapshotRunner;
        private readonly IPortfolioSnapshotQueryService _portfolioSnapshotQueryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioSnapshotController"/>.
        /// </summary>
        /// <param name="portfolioSnapshotRunner">
        /// Service used to execute portfolio value snapshots for all portfolios.
        /// </param>
        public PortfolioSnapshotController(PortfolioSnapshotRunner portfolioSnapshotRunner,  IPortfolioSnapshotQueryService portfolioSnapshotQueryService)
        {
            _portfolioSnapshotRunner = portfolioSnapshotRunner;
            _portfolioSnapshotQueryService = portfolioSnapshotQueryService;
        }

        /// <summary>
        /// Take a portfolio value snapshot for a specific trading date.
        /// </summary>
        /// <remarks>
        /// Fetches all active portfolios, retrieves the latest closing price for each
        /// held symbol, and upserts a snapshot record capturing the total and per-holding
        /// value of each portfolio on the given trading date.
        ///
        /// This endpoint is primarily intended for manual testing and operational recovery,
        /// such as re-running a missed daily snapshot.
        /// </remarks>
        /// <param name="date">
        /// The trading date to snapshot in yyyy-MM-dd format.
        /// </param>
        /// <param name="ct">
        /// Cancellation token for the request.
        /// </param>
        /// <returns>A confirmation that the snapshots were saved successfully.</returns>
        /// <response code="200">The portfolio snapshots were saved successfully.</response>
        /// <response code="400">The provided date was invalid.</response>
        /// <response code="500">The portfolio snapshot job failed.</response>
        [HttpPost("{date}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> TakeSnapshot(
            [FromRoute] DateOnly date,
            CancellationToken ct)
        {
            await _portfolioSnapshotRunner.UpsertAllPortfolioSnapshots(date, ct);

            return Ok(new
            {
                message = "Portfolio snapshots added to database.",
                date
            });
        }
        
        /// <summary>
        /// Retrieve historical portfolio value snapshots.
        /// </summary>
        /// <remarks>
        /// Returns daily portfolio value snapshots for the specified portfolio,
        /// including per-holding breakdowns.
        ///
        /// Optional date filters can be used to restrict the range.
        /// Results are sorted in ascending chronological order (oldest → newest).
        /// </remarks>
        /// <param name="portfolioId">The portfolio's unique identifier.</param>
        /// <param name="from">Optional start date (inclusive).</param>
        /// <param name="to">Optional end date (inclusive).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>Historical daily portfolio value snapshots.</returns>
        /// <response code="200">Snapshots retrieved successfully.</response>
        /// <response code="404">Portfolio not found.</response>
        [HttpGet("{portfolioId}")]
        [ProducesResponseType(typeof(PortfolioSnapshotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSnapshots(
            [FromRoute] Guid portfolioId,
            [FromQuery] DateOnly? from = null,
            [FromQuery] DateOnly? to = null,
            CancellationToken ct = default)
        {
            var resp = await _portfolioSnapshotQueryService.GetSnapshotsForPortfolioAsync(
                portfolioId, from, to, ct);
            return Ok(resp);
        }
    }
}