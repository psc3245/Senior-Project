using StockTraderBackend.Assets.Symbols.DTOs;

namespace StockTraderBackend.Assets.Symbols
{
    public interface ISymbolsRepository
    {
        Task<SymbolRow?> GetByTickerAsync(string ticker, CancellationToken ct);
        Task<List<SymbolRow>> GetActiveSymbolsAsync(CancellationToken ct = default);
        Task<List<SymbolDetailsRow>> GetAllSymbolsAsync(CancellationToken ct = default);
        Task<SymbolDetailsRow?> GetDetailsByTickerAsync(string ticker, CancellationToken ct = default);
        Task<SymbolDetailsRow?> GetDetailsByCompanyNameAsync(string companyName, CancellationToken ct = default);
        Task<List<string>> GetAllCompanyNamesAsync(CancellationToken ct = default);

        Task<SymbolRow?> GetByIdAsync(int symbolId, CancellationToken ct = default);
        Task<List<SymbolBackfillRow>> GetSymbolsForBackfillScanAsync(CancellationToken ct = default);
        Task MarkBackfillAttemptAsync(int symbolId, CancellationToken ct = default);
        Task MarkBackfillSuccessAsync(int symbolId, CancellationToken ct = default);
        Task MarkNeedsBackfillAsync(int symbolId, CancellationToken ct = default);
        Task<List<SymbolRow>> GetActiveSymbolsByTickersAsync(IEnumerable<string> tickers,CancellationToken ct = default);
        Task MarkSplitScanUtcAsync(int symbolId,CancellationToken ct = default);
        Task MarkSplitDetectedAsync(int symbolId,CancellationToken ct = default);
        Task<List<SymbolResponseDto>> GetRandomSubsetOfSymbols(int count, CancellationToken ct = default);
    }

    public sealed record SymbolRow(int Id, string Ticker);

    public sealed record SymbolBackfillRow(
        int Id,
        string Ticker,
        bool IsActive,
        bool NeedsBackfill,
        DateTime? LastBackfillAttemptUtc,
        DateTime? LastBackfillSuccessUtc
    );

    public sealed record SymbolDetailsRow(
        int Id,
        string Ticker,
        string Name,
        string Exchange,
        AssetTypes AssetType,
        bool IsActive,
        DateTime CreatedAt
    );
}