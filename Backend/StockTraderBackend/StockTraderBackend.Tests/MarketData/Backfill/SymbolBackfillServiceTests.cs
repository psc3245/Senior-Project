using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StockTraderBackend.Assets.PriceBar;
using StockTraderBackend.Assets.PriceBar.Models;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.MarketData.Backfill;
using StockTraderBackend.MarketData.Calendar;
using StockTraderBackend.MarketData.Massive;
using StockTraderBackend.MarketData.Massive.DTOs;
using Xunit;

namespace StockTraderBackend.Tests.MarketData.Backfill
{
    public sealed class SymbolBackfillServiceTests
    {
        private readonly Mock<ISymbolsRepository> _symbolsRepository = new();
        private readonly Mock<IPriceBarsRepository> _priceBarsRepository = new();
        private readonly Mock<IMassiveMarketDataClient> _massiveClient = new();
        private readonly Mock<IMarketCalendarService> _marketCalendarService = new();
        private readonly Mock<ILogger<SymbolBackfillService>> _logger = new();

        private SymbolBackfillService CreateService()
        {
            var options = Options.Create(new BackfillOptions
            {
                RequestDelay = TimeSpan.Zero,
                RetryDelay = TimeSpan.Zero,
                MaxRetries = 2,
                BackfillYears = 2
            });

            return new SymbolBackfillService(
                _symbolsRepository.Object,
                _priceBarsRepository.Object,
                _massiveClient.Object,
                _marketCalendarService.Object,
                options,
                _logger.Object);
        }

        [Fact]
        public async Task BackfillSymbolAsync_ReturnsImmediately_WhenSymbolDoesNotExist()
        {
            var service = CreateService();

            _symbolsRepository
                .Setup(x => x.GetByIdAsync(123, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SymbolRow?)null);

            await service.BackfillSymbolAsync(123);

            _symbolsRepository.Verify(
                x => x.MarkBackfillAttemptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _massiveClient.VerifyNoOtherCalls();
            _priceBarsRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BackfillSymbolAsync_FetchesFullRange_WhenCoverageIsMissing()
        {
            var service = CreateService();
            const int symbolId = 1;

            var latestTradingDayDateTime = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);
            var latestTradingDay = DateOnly.FromDateTime(latestTradingDayDateTime);
            var expectedOldest = latestTradingDay.AddYears(-2);

            _symbolsRepository
                .Setup(x => x.GetByIdAsync(symbolId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolRow(symbolId, "AAPL"));

            _symbolsRepository
                .Setup(x => x.MarkBackfillAttemptAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _symbolsRepository
                .Setup(x => x.MarkBackfillSuccessAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _marketCalendarService
                .Setup(x => x.GetMostRecentTradingDay(It.IsAny<DateTime>()))
                .Returns(latestTradingDayDateTime);

            _priceBarsRepository
                .Setup(x => x.GetCoverageAsync(symbolId, "1d", It.IsAny<CancellationToken>()))
                .ReturnsAsync((PriceBarCoverageRow?)null);

            _massiveClient
                .Setup(x => x.GetDailyBarsAsync(
                    "AAPL",
                    expectedOldest,
                    latestTradingDay,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MassiveTickerRangeResponse
                {
                    Results = new List<MassiveTickerRangeBarDto>
                    {
                        new MassiveTickerRangeBarDto
                        {
                            TimestampUnixMs = new DateTimeOffset(
                                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc))
                                .ToUnixTimeMilliseconds(),
                            Open = 100m,
                            High = 110m,
                            Low = 95m,
                            Close = 108m,
                            Volume = 1000m
                        }
                    }
                });

            _priceBarsRepository
                .Setup(x => x.UpsertBarsAsync(
                    It.IsAny<IEnumerable<PriceBar>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await service.BackfillSymbolAsync(symbolId);

            _symbolsRepository.Verify(
                x => x.MarkBackfillAttemptAsync(symbolId, It.IsAny<CancellationToken>()),
                Times.Once);

            _massiveClient.Verify(
                x => x.GetDailyBarsAsync(
                    "AAPL",
                    expectedOldest,
                    latestTradingDay,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _priceBarsRepository.Verify(
                x => x.UpsertBarsAsync(
                    It.Is<IEnumerable<PriceBar>>(bars =>
                        bars.Count() == 1 &&
                        bars.First().SymbolId == symbolId &&
                        bars.First().Timeframe == "1d" &&
                        bars.First().Open == 100m &&
                        bars.First().Close == 108m &&
                        bars.First().Volume == 1000m),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _symbolsRepository.Verify(
                x => x.MarkBackfillSuccessAsync(symbolId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BackfillSymbolAsync_DoesNotUpsert_WhenProviderReturnsNoBars()
        {
            var service = CreateService();
            const int symbolId = 1;

            var latestTradingDayDateTime = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);
            var latestTradingDay = DateOnly.FromDateTime(latestTradingDayDateTime);
            var expectedOldest = latestTradingDay.AddYears(-2);

            _symbolsRepository
                .Setup(x => x.GetByIdAsync(symbolId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolRow(symbolId, "AAPL"));

            _symbolsRepository
                .Setup(x => x.MarkBackfillAttemptAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _symbolsRepository
                .Setup(x => x.MarkBackfillSuccessAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _marketCalendarService
                .Setup(x => x.GetMostRecentTradingDay(It.IsAny<DateTime>()))
                .Returns(latestTradingDayDateTime);

            _priceBarsRepository
                .Setup(x => x.GetCoverageAsync(symbolId, "1d", It.IsAny<CancellationToken>()))
                .ReturnsAsync((PriceBarCoverageRow?)null);

            _massiveClient
                .Setup(x => x.GetDailyBarsAsync(
                    "AAPL",
                    expectedOldest,
                    latestTradingDay,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MassiveTickerRangeResponse
                {
                    Results = new List<MassiveTickerRangeBarDto>()
                });

            await service.BackfillSymbolAsync(symbolId);

            _priceBarsRepository.Verify(
                x => x.UpsertBarsAsync(
                    It.IsAny<IEnumerable<PriceBar>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _symbolsRepository.Verify(
                x => x.MarkBackfillSuccessAsync(symbolId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BackfillSymbolAsync_Retries_WhenProviderFailsOnce()
        {
            var service = CreateService();
            const int symbolId = 1;
            var callCount = 0;

            var latestTradingDayDateTime = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc);
            var latestTradingDay = DateOnly.FromDateTime(latestTradingDayDateTime);
            var expectedOldest = latestTradingDay.AddYears(-2);

            _symbolsRepository
                .Setup(x => x.GetByIdAsync(symbolId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SymbolRow(symbolId, "AAPL"));

            _symbolsRepository
                .Setup(x => x.MarkBackfillAttemptAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _symbolsRepository
                .Setup(x => x.MarkBackfillSuccessAsync(symbolId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _marketCalendarService
                .Setup(x => x.GetMostRecentTradingDay(It.IsAny<DateTime>()))
                .Returns(latestTradingDayDateTime);

            _priceBarsRepository
                .Setup(x => x.GetCoverageAsync(symbolId, "1d", It.IsAny<CancellationToken>()))
                .ReturnsAsync((PriceBarCoverageRow?)null);

            _massiveClient
                .Setup(x => x.GetDailyBarsAsync(
                    "AAPL",
                    expectedOldest,
                    latestTradingDay,
                    It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    callCount++;

                    if (callCount == 1)
                    {
                        throw new HttpRequestException("Transient failure");
                    }

                    return new MassiveTickerRangeResponse
                    {
                        Results = new List<MassiveTickerRangeBarDto>
                        {
                            new MassiveTickerRangeBarDto
                            {
                                TimestampUnixMs = new DateTimeOffset(
                                    new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc))
                                    .ToUnixTimeMilliseconds(),
                                Open = 100m,
                                High = 110m,
                                Low = 95m,
                                Close = 108m,
                                Volume = 1000m
                            }
                        }
                    };
                });

            _priceBarsRepository
                .Setup(x => x.UpsertBarsAsync(
                    It.IsAny<IEnumerable<PriceBar>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await service.BackfillSymbolAsync(symbolId);

            Assert.Equal(2, callCount);

            _priceBarsRepository.Verify(
                x => x.UpsertBarsAsync(
                    It.IsAny<IEnumerable<PriceBar>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}