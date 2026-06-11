using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;

namespace HomemadeCookie.Api.Patterns.Facade;

public class InventorySystem
{
    private readonly CookieRepository _cookieRepository;

    public InventorySystem(CookieRepository cookieRepository)
    {
        _cookieRepository = cookieRepository;
    }

    public async Task<(bool Ok, string? OutOfStockProduct)> CheckStockAsync(
        IReadOnlyList<CartLine> lines,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            var cookie = await _cookieRepository.GetByIdAsync(line.CookieId, cancellationToken);
            if (cookie is null)
                return (false, line.CookieName);

            if (cookie.Stock < line.Quantity)
                return (false, cookie.Name);
        }

        return (true, null);
    }

    public Task ReduceStockAsync(IReadOnlyList<CartLine> lines, CancellationToken cancellationToken = default)
    {
        var reductions = lines.Select(l => (l.CookieId, l.Quantity)).ToList();
        return _cookieRepository.ReduceStockAsync(reductions, cancellationToken);
    }
}
