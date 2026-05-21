using System.Security.Claims;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly CartRepository _cartRepository;

    public CartController(CartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> GetCart(int customerId, CancellationToken cancellationToken)
    {
        if (!OwnsCart(customerId))
            return Forbid();

        var items = await _cartRepository.GetItemsAsync(customerId, cancellationToken);
        var total = items.Sum(i => i.LineTotal);

        return Ok(new
        {
            customerId,
            items,
            itemCount = items.Sum(i => i.Quantity),
            total
        });
    }

    [HttpPost("{customerId:int}/items")]
    public async Task<IActionResult> AddItem(
        int customerId,
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!OwnsCart(customerId))
            return Forbid();

        if (request.CookieId <= 0)
            return BadRequest(new { message = "CookieId must be a positive integer." });

        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be positive." });

        try
        {
            await _cartRepository.AddItemAsync(customerId, request.CookieId, request.Quantity, cancellationToken);
            var items = await _cartRepository.GetItemsAsync(customerId, cancellationToken);
            return Ok(new
            {
                message = "Item added to cart.",
                customerId,
                items,
                total = items.Sum(i => i.LineTotal)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{customerId:int}/items/{cookieId:int}")]
    public async Task<IActionResult> UpdateItem(
        int customerId,
        int cookieId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!OwnsCart(customerId))
            return Forbid();

        if (request.Quantity < 0)
            return BadRequest(new { message = "Quantity cannot be negative." });

        try
        {
            if (request.Quantity == 0)
                await _cartRepository.RemoveItemAsync(customerId, cookieId, cancellationToken);
            else
                await _cartRepository.SetItemQuantityAsync(customerId, cookieId, request.Quantity, cancellationToken);

            var items = await _cartRepository.GetItemsAsync(customerId, cancellationToken);
            return Ok(new
            {
                message = request.Quantity == 0 ? "Item removed from cart." : "Cart updated.",
                customerId,
                items,
                total = items.Sum(i => i.LineTotal)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{customerId:int}/items/{cookieId:int}")]
    public async Task<IActionResult> RemoveItem(
        int customerId,
        int cookieId,
        CancellationToken cancellationToken)
    {
        if (!OwnsCart(customerId))
            return Forbid();

        await _cartRepository.RemoveItemAsync(customerId, cookieId, cancellationToken);
        var items = await _cartRepository.GetItemsAsync(customerId, cancellationToken);

        return Ok(new
        {
            message = "Item removed from cart.",
            customerId,
            items,
            total = items.Sum(i => i.LineTotal)
        });
    }

    private bool OwnsCart(int customerId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var id) && id == customerId;
    }
}
