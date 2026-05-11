using StockTraderBackend.Assets.Symbols.DTOs;

namespace StockTraderBackend.Assets.Symbols.Service;

public sealed class SymbolsService : ISymbolsService
{
    private readonly ISymbolsRepository _repository;

    public SymbolsService(ISymbolsRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SymbolResponseDto>> GetAllSymbolsAsync(CancellationToken ct = default)
    {
        var rows = await _repository.GetAllSymbolsAsync(ct);

        return rows.Select(MapToDto).ToList();
    }

    public async Task<SymbolResponseDto?> GetSymbolByTickerAsync(string ticker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return null;
        }

        var row = await _repository.GetDetailsByTickerAsync(ticker, ct);
        return row is null ? null : MapToDto(row);
    }

    public async Task<SymbolResponseDto?> GetSymbolByCompanyNameAsync(string companyName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return null;
        }

        var row = await _repository.GetDetailsByCompanyNameAsync(companyName, ct);
        return row is null ? null : MapToDto(row);
    }

    public Task<List<string>> GetAllCompanyNamesAsync(CancellationToken ct = default)
    {
        return _repository.GetAllCompanyNamesAsync(ct);
    }
    public async Task<List<SymbolResponseDto>> GetRandomSubsetOfSymbols(int count,CancellationToken ct = default)
    {
        return await _repository.GetRandomSubsetOfSymbols(count, ct);
    }
    private static SymbolResponseDto MapToDto(SymbolDetailsRow row)
    {
        return new SymbolResponseDto
        {
            Id = row.Id,
            Ticker = row.Ticker,
            Name = row.Name,
            Exchange = row.Exchange,
            AssetType = row.AssetType,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt
        };
    }
}