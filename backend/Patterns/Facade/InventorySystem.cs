using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;

namespace HomemadeCookie.Api.Patterns.Facade;

public class InventorySystem
{
    private readonly CookieRepository _cookieRepository;
    private readonly ILogger<InventorySystem> _logger;

    public InventorySystem(CookieRepository cookieRepository, ILogger<InventorySystem> logger)
    {
        _cookieRepository = cookieRepository;
        _logger = logger;
    }

    public async Task<(bool Ok, string? OutOfStockProduct)> CheckStockAsync(
        IReadOnlyList<CartLine> lines,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            var cookie = await _cookieRepository.GetByIdAsync(line.CookieId, cancellationToken);
            if (cookie is null)
                return (false, line.CookieName ?? "Unknown product");

            if (cookie.Stock < line.Quantity)
                return (false, cookie.Name ?? "Unknown product");
        }

        return (true, null);
    }

    public async Task ReduceStockAsync(
        IReadOnlyList<CartLine> lines, 
        CancellationToken cancellationToken = default)
    {
        var reductions = lines.Select(l => (l.CookieId, l.Quantity)).ToList();
        await _cookieRepository.ReduceStockAsync(reductions, cancellationToken);
        
        _logger.LogInformation("Stock reduced for {Count} products", reductions.Count);
    }

    /// <summary>
    /// Release stock back to inventory (for cancelled orders)
    /// </summary>
    public async Task ReleaseStockAsync(
        IReadOnlyList<CartLine> lines, 
        CancellationToken cancellationToken = default)
    {
        if (lines == null || !lines.Any())
            return;

        foreach (var line in lines)
        {
            await ReleaseStockAsync(line.CookieId, line.Quantity, cancellationToken);
        }
        
        _logger.LogInformation("Stock released for {Count} products", lines.Count);
    }

    /// <summary>
    /// Release stock for a single product
    /// </summary>
    public async Task ReleaseStockAsync(
        int cookieId, 
        int quantity, 
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return;

        var cookie = await _cookieRepository.GetByIdAsync(cookieId, cancellationToken);
        if (cookie == null)
        {
            _logger.LogWarning("Cookie {CookieId} not found when trying to release stock", cookieId);
            return;
        }

        // Increase stock by the released quantity
        var newStock = cookie.Stock + quantity;
        await _cookieRepository.UpdateStockAsync(cookieId, newStock, cancellationToken);
        
        _logger.LogInformation(
            "Released {Quantity} units of {CookieName} (Cookie #{CookieId}). New stock: {NewStock}", 
            quantity, 
            cookie.Name ?? "Unknown", 
            cookieId, 
            newStock);
    }

    /// <summary>
    /// Check if there's enough stock for a single product
    /// </summary>
    public async Task<bool> CheckStockAsync(
        int cookieId, 
        int quantity, 
        CancellationToken cancellationToken = default)
    {
        var cookie = await _cookieRepository.GetByIdAsync(cookieId, cancellationToken);
        if (cookie == null)
            return false;

        return cookie.Stock >= quantity;
    }

    /// <summary>
    /// Get current stock level for a product
    /// </summary>
    public async Task<int> GetStockLevelAsync(
        int cookieId, 
        CancellationToken cancellationToken = default)
    {
        var cookie = await _cookieRepository.GetByIdAsync(cookieId, cancellationToken);
        return cookie?.Stock ?? 0;
    }
}