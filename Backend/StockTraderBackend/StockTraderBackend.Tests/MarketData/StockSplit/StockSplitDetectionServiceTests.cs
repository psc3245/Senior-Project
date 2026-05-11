using Microsoft.Extensions.Logging;
using Moq;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.MarketData.Massive;
using StockTraderBackend.MarketData.Massive.DTOs;
using StockTraderBackend.MarketData.StockSplits.Models;
using StockTraderBackend.MarketData.StockSplits.Repository;
using StockTraderBackend.MarketData.StockSplits.Service;


namespace StockTraderBackend.MarketData.StockSplits;

public sealed class StockSplitDetectionServiceTests
{
    private readonly Mock<IMassiveCorporateActionsClient> _massiveCorporateActionsClient = new();
    private readonly Mock<IStockSplitsRepository> _stockSplitsRepository = new();
    private readonly Mock<ISymbolsRepository> _symbolsRepository = new();
    private readonly Mock<ILogger<StockSplitDetectionService>> _logger = new();

    private StockSplitDetectionService CreateService()
    {
        return new StockSplitDetectionService(
            _massiveCorporateActionsClient.Object,
            _stockSplitsRepository.Object,
            _symbolsRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ScanRecentSplitsAsync_AddsNewSplit_AndMarksSymbolForBackfill()
    {
        var service = CreateService();

        var symbolId = 1;
        var ticker = "AAPL";
        var splitDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

        _massiveCorporateActionsClient
            .Setup(x => x.GetSplitsAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MassiveSplitDto>
            {
                new MassiveSplitDto
                {
                    Id = "split_1",
                    Ticker = ticker,
                    ExecutionDate = splitDate,
                    SplitFrom = 1m,
                    SplitTo = 4m
                }
            });

        _symbolsRepository
            .Setup(x => x.GetActiveSymbolsByTickersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SymbolRow>
            {
                new SymbolRow(symbolId, ticker)
            });

        _stockSplitsRepository
            .Setup(x => x.ExistsByMassiveSplitIdAsync("split_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _stockSplitsRepository
            .Setup(x => x.AddAsync(It.IsAny<StockSplit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _stockSplitsRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _symbolsRepository
            .Setup(x => x.MarkNeedsBackfillAsync(symbolId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _symbolsRepository
            .Setup(x => x.MarkSplitDetectedAsync(symbolId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _symbolsRepository
            .Setup(x => x.MarkSplitScanUtcAsync(symbolId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.ScanRecentSplitsAsync(14);

        Assert.Equal(1, result);

        _stockSplitsRepository.Verify(
            x => x.AddAsync(
                It.Is<StockSplit>(s =>
                    s.SymbolId == symbolId &&
                    s.EffectiveDate == splitDate &&
                    s.SplitFrom == 1m &&
                    s.SplitTo == 4m &&
                    s.SplitFactor == 4m &&
                    s.MassiveSplitId == "split_1"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _stockSplitsRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _symbolsRepository.Verify(
            x => x.MarkNeedsBackfillAsync(symbolId, It.IsAny<CancellationToken>()),
            Times.Once);

        _symbolsRepository.Verify(
            x => x.MarkSplitDetectedAsync(symbolId, It.IsAny<CancellationToken>()),
            Times.Once);

        _symbolsRepository.Verify(
            x => x.MarkSplitScanUtcAsync(symbolId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScanRecentSplitsAsync_DoesNothing_WhenSplitAlreadyExists()
    {
        var service = CreateService();

        var symbolId = 1;
        var ticker = "AAPL";
        var splitDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

        _massiveCorporateActionsClient
            .Setup(x => x.GetSplitsAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MassiveSplitDto>
            {
                new MassiveSplitDto
                {
                    Id = "split_1",
                    Ticker = ticker,
                    ExecutionDate = splitDate,
                    SplitFrom = 1m,
                    SplitTo = 4m
                }
            });

        _symbolsRepository
            .Setup(x => x.GetActiveSymbolsByTickersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SymbolRow>
            {
                new SymbolRow(symbolId, ticker)
            });

        _stockSplitsRepository
            .Setup(x => x.ExistsByMassiveSplitIdAsync("split_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _symbolsRepository
            .Setup(x => x.MarkSplitScanUtcAsync(symbolId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.ScanRecentSplitsAsync(14);

        Assert.Equal(0, result);

        _stockSplitsRepository.Verify(
            x => x.AddAsync(It.IsAny<StockSplit>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkNeedsBackfillAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkSplitDetectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkSplitScanUtcAsync(symbolId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScanRecentSplitsAsync_IgnoresUntrackedTicker()
    {
        var service = CreateService();

        var splitDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

        _massiveCorporateActionsClient
            .Setup(x => x.GetSplitsAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MassiveSplitDto>
            {
                new MassiveSplitDto
                {
                    Id = "split_1",
                    Ticker = "AAPL",
                    ExecutionDate = splitDate,
                    SplitFrom = 1m,
                    SplitTo = 4m
                }
            });

        _symbolsRepository
            .Setup(x => x.GetActiveSymbolsByTickersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SymbolRow>());

        var result = await service.ScanRecentSplitsAsync(14);

        Assert.Equal(0, result);

        _stockSplitsRepository.Verify(
            x => x.AddAsync(It.IsAny<StockSplit>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkNeedsBackfillAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanRecentSplitsAsync_IgnoresTodayOrFutureSplits()
    {
        var service = CreateService();

        var symbolId = 1;
        var ticker = "AAPL";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _massiveCorporateActionsClient
            .Setup(x => x.GetSplitsAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MassiveSplitDto>
            {
                new MassiveSplitDto
                {
                    Id = "split_today",
                    Ticker = ticker,
                    ExecutionDate = today,
                    SplitFrom = 1m,
                    SplitTo = 4m
                }
            });

        _symbolsRepository
            .Setup(x => x.GetActiveSymbolsByTickersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SymbolRow>
            {
                new SymbolRow(symbolId, ticker)
            });

        var result = await service.ScanRecentSplitsAsync(14);

        Assert.Equal(0, result);

        _stockSplitsRepository.Verify(
            x => x.AddAsync(It.IsAny<StockSplit>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkNeedsBackfillAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkSplitDetectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _symbolsRepository.Verify(
            x => x.MarkSplitScanUtcAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSplitsForTickerAsync_ReturnsEmpty_WhenSymbolDoesNotExist()
    {
        var service = CreateService();

        _symbolsRepository
            .Setup(x => x.GetByTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SymbolRow?)null);

        var result = await service.GetSplitsForTickerAsync("AAPL");

        Assert.Empty(result);

        _stockSplitsRepository.Verify(
            x => x.GetBySymbolIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSplitsForTickerAsync_ReturnsSplits_WhenSymbolExists()
    {
        var service = CreateService();

        _symbolsRepository
            .Setup(x => x.GetByTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SymbolRow(1, "AAPL"));

        _stockSplitsRepository
            .Setup(x => x.GetBySymbolIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockSplit>
            {
                new StockSplit
                {
                    Id = 10,
                    SymbolId = 1,
                    EffectiveDate = new DateOnly(2024, 1, 1),
                    SplitFrom = 1m,
                    SplitTo = 4m,
                    SplitFactor = 4m
                }
            });

        var result = await service.GetSplitsForTickerAsync("AAPL");

        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }

    [Fact]
    public async Task DeleteSplitAsync_ReturnsFalse_WhenSplitDoesNotExist()
    {
        var service = CreateService();

        _stockSplitsRepository
            .Setup(x => x.GetByIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockSplit?)null);

        var result = await service.DeleteSplitAsync(123);

        Assert.False(result);

        _stockSplitsRepository.Verify(
            x => x.Remove(It.IsAny<StockSplit>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteSplitAsync_RemovesSplit_WhenItExists()
    {
        var service = CreateService();

        var split = new StockSplit
        {
            Id = 123,
            SymbolId = 1,
            EffectiveDate = new DateOnly(2024, 1, 1),
            SplitFrom = 1m,
            SplitTo = 4m,
            SplitFactor = 4m
        };

        _stockSplitsRepository
            .Setup(x => x.GetByIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(split);

        _stockSplitsRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.DeleteSplitAsync(123);

        Assert.True(result);

        _stockSplitsRepository.Verify(
            x => x.Remove(split),
            Times.Once);

        _stockSplitsRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}