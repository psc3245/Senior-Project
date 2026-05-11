namespace StockTraderBackend.Assets.Symbols.Service
{
    using StockTraderBackend.Assets.Symbols.DTOs;

    public interface ISymbolsService
    {
        Task<List<SymbolResponseDto>> GetAllSymbolsAsync(CancellationToken ct = default);
        Task<SymbolResponseDto?> GetSymbolByTickerAsync(string ticker, CancellationToken ct = default);
        Task<SymbolResponseDto?> GetSymbolByCompanyNameAsync(string companyName, CancellationToken ct = default);
        Task<List<string>> GetAllCompanyNamesAsync(CancellationToken ct = default);
        Task<List<SymbolResponseDto>> GetRandomSubsetOfSymbols(int count, CancellationToken ct = default);
    }
}
