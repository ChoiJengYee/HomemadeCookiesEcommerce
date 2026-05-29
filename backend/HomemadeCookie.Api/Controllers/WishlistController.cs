using System.Security.Claims;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize(Roles = "Customer")]
public class WishlistController : ControllerBase
{
    private readonly WishlistRepository _wishlist;
    private readonly CartRepository _cart;

    public WishlistController(WishlistRepository wishlist, CartRepository cart)
    {
        _wishlist = wishlist;
        _cart = cart;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var items = await _wishlist.GetItemsAsync(customerId.Value, cancellationToken);
        return Ok(new { customerId = customerId.Value, items, itemCount = items.Count });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] WishlistItemRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        if (request.CookieId <= 0)
            return BadRequest(new { message = "CookieId is required." });

        await _wishlist.AddItemAsync(customerId.Value, request.CookieId, cancellationToken);
        var items = await _wishlist.GetItemsAsync(customerId.Value, cancellationToken);
        return Ok(new { message = "Added to wishlist.", customerId = customerId.Value, items });
    }

    [HttpDelete("items/{cookieId:int}")]
    public async Task<IActionResult> RemoveItem(int cookieId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        await _wishlist.RemoveItemAsync(customerId.Value, cookieId, cancellationToken);
        var items = await _wishlist.GetItemsAsync(customerId.Value, cancellationToken);
        return Ok(new { message = "Removed from wishlist.", customerId = customerId.Value, items });
    }

    [HttpPost("move-to-cart")]
    public async Task<IActionResult> MoveToCart(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var items = await _wishlist.GetItemsAsync(customerId.Value, cancellationToken);
        var moved = 0;

        foreach (var item in items)
        {
            try
            {
                await _cart.AddItemAsync(customerId.Value, item.CookieId, 1, cancellationToken);
                await _wishlist.RemoveItemAsync(customerId.Value, item.CookieId, cancellationToken);
                moved++;
            }
            catch (InvalidOperationException)
            {
                // Skip items that are out of stock
            }
        }

        var wishlist = await _wishlist.GetItemsAsync(customerId.Value, cancellationToken);
        var cart = await _cart.GetItemsAsync(customerId.Value, cancellationToken);

        return Ok(new
        {
            message = $"Moved {moved} item(s) to cart.",
            moved,
            wishlist,
            cart = new
            {
                customerId = customerId.Value,
                items = cart,
                total = cart.Sum(i => i.LineTotal)
            }
        });
    }

    private int? GetCustomerId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}

public class WishlistItemRequest
{
    public int CookieId { get; set; }
}
