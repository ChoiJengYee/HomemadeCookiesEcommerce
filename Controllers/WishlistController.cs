using System.Security.Claims;
using System.Text.Json;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly WishlistRepository _wishlist;
    private readonly CartRepository _cartRepository;

    public WishlistController(WishlistRepository wishlist, CartRepository cartRepository)
    {
        _wishlist = wishlist;
        _cartRepository = cartRepository;
    }

    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> Get(int customerId, CancellationToken cancellationToken)
    {
        var items = await _wishlist.GetItemsAsync(customerId, cancellationToken);
        return Ok(items);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var customerId))
            return Unauthorized();

        var items = await _wishlist.GetItemsAsync(customerId, cancellationToken);
        return Ok(items);
    }

    private static bool TryReadCookieId(JsonElement body, out int cookieId)
    {
        if (body.ValueKind == JsonValueKind.Number && body.TryGetInt32(out cookieId))
            return true;

        if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("cookieId", out var cookieIdElement) && cookieIdElement.ValueKind == JsonValueKind.Number)
            return cookieIdElement.TryGetInt32(out cookieId);

        cookieId = 0;
        return false;
    }

    [Authorize]
    [HttpPost("{customerId:int}/items")]
    public async Task<IActionResult> Add(int customerId, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!TryReadCookieId(body, out var cookieId))
            return BadRequest(new { message = "cookieId is required." });

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (userId != customerId && !User.IsInRole("Admin"))
            return Forbid();

        await _wishlist.AddItemAsync(customerId, cookieId, cancellationToken);
        return Ok(new { message = "Added to wishlist." });
    }

    [Authorize]
    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!TryReadCookieId(body, out var cookieId))
            return BadRequest(new { message = "cookieId is required." });

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var customerId))
            return Unauthorized();

        await _wishlist.AddItemAsync(customerId, cookieId, cancellationToken);
        return Ok(new { message = "Added to wishlist." });
    }

    [Authorize]
    [HttpDelete("{customerId:int}/items/{cookieId:int}")]
    public async Task<IActionResult> Remove(int customerId, int cookieId, CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (userId != customerId && !User.IsInRole("Admin"))
            return Forbid();

        await _wishlist.RemoveItemAsync(customerId, cookieId, cancellationToken);
        return Ok(new { message = "Removed from wishlist." });
    }

    [Authorize]
    [HttpDelete("items/{cookieId:int}")]
    public async Task<IActionResult> Remove(int cookieId, CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var customerId))
            return Unauthorized();

        await _wishlist.RemoveItemAsync(customerId, cookieId, cancellationToken);
        return Ok(new { message = "Removed from wishlist." });
    }

    [Authorize]
    [HttpPost("{customerId:int}/move-to-cart")]
    public async Task<IActionResult> MoveToCart(int customerId, CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (userId != customerId && !User.IsInRole("Admin"))
            return Forbid();

        var items = await _wishlist.GetItemsAsync(customerId, cancellationToken);
        foreach (var item in items)
        {
            await _cartRepository.AddItemAsync(customerId, item.CookieId, 1, cancellationToken);
        }

        await _wishlist.ClearAsync(customerId, cancellationToken);
        return Ok(new { message = "Moved wishlist items to cart." });
    }

    [Authorize]
    [HttpPost("move-to-cart")]
    public async Task<IActionResult> MoveToCart(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var customerId))
            return Unauthorized();

        var items = await _wishlist.GetItemsAsync(customerId, cancellationToken);
        foreach (var item in items)
        {
            await _cartRepository.AddItemAsync(customerId, item.CookieId, 1, cancellationToken);
        }

        await _wishlist.ClearAsync(customerId, cancellationToken);
        return Ok(new { message = "Moved wishlist items to cart." });
    }
}
