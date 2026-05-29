using System.Security.Claims;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly WishlistRepository _wishlist;

    public WishlistController(WishlistRepository wishlist)
    {
        _wishlist = wishlist;
    }

    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> Get(int customerId, CancellationToken cancellationToken)
    {
        var items = await _wishlist.GetItemsAsync(customerId, cancellationToken);
        return Ok(items);
    }

    [Authorize]
    [HttpPost("{customerId:int}/items")]
    public async Task<IActionResult> Add(int customerId, [FromBody] int cookieId, CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (userId != customerId && !User.IsInRole("Admin"))
            return Forbid();

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
}
