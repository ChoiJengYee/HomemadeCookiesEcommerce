using System.Security.Claims;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Patterns.Facade;
using HomemadeCookie.Api.Patterns.State;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderManagementFacade _facade;
    private readonly OrderRepository _orders;

    public OrdersController(OrderManagementFacade facade, OrderRepository orders)
    {
        _facade = facade;
        _orders = orders;
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetUserId();
        if (customerId is null)
            return Unauthorized();

        request.CustomerId = customerId.Value;

        var result = await _facade.PlaceOrderAsync(request, cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Outcome switch
        {
            "OutOfStock" => UnprocessableEntity(result),
            "PaymentPending" => Ok(result),
            _ => BadRequest(result)
        };
    }

    [HttpGet("{id:int}/status")]
    public async Task<IActionResult> GetStatus(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound(new { message = $"Order {id} not found." });

        if (!CanAccessOrder(order.CustomerId))
            return Forbid();

        var context = OrderStateFactory.FromOrder(order.OrderId, order.CustomerId, order.StatusId);

        return Ok(new
        {
            orderId = order.OrderId,
            statusId = order.StatusId,
            statusName = context.GetStatus(),
            dbStatusName = order.StatusName,
            orderDate = order.OrderDate,
            totalAmount = order.TotalAmount,
            canCancel = order.StatusId == OrderStatusIds.Pending,
            items = await _orders.GetItemsAsync(id, cancellationToken)
        });
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound(new { message = $"Order {id} not found." });

        if (!CanAccessOrder(order.CustomerId))
            return Forbid();

        var context = OrderStateFactory.FromOrder(order.OrderId, order.CustomerId, order.StatusId);

        try
        {
            context.RequestCancel();
            await _orders.UpdateStatusAsync(order.OrderId, context.StatusId, cancellationToken);

            return Ok(new
            {
                orderId = order.OrderId,
                statusId = context.StatusId,
                statusName = context.GetStatus(),
                message = "Order cancelled successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    private bool CanAccessOrder(int orderCustomerId)
    {
        if (User.IsInRole("Admin"))
            return true;

        return GetUserId() == orderCustomerId;
    }
}
