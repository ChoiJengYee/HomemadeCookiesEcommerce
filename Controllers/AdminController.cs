using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Patterns.Factory;
using HomemadeCookie.Api.Patterns.State;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly CookieRepository _cookieRepository;
    private readonly OrderRepository _orderRepository;

    public AdminController(CookieRepository cookieRepository, OrderRepository orderRepository)
    {
        _cookieRepository = cookieRepository;
        _orderRepository = orderRepository;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpPut("orders/{id:int}/advance")]
    public async Task<IActionResult> AdvanceOrder(int id, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound(new { message = $"Order {id} not found." });

        var context = OrderStateFactory.FromOrder(order.OrderId, order.CustomerId, order.StatusId);

        try
        {
            context.Proceed();
            await _orderRepository.UpdateStatusAsync(order.OrderId, context.StatusId, cancellationToken);

            return Ok(new
            {
                orderId = order.OrderId,
                statusId = context.StatusId,
                statusName = context.GetStatus(),
                message = "Status updated successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cookies")]
    public async Task<IActionResult> CreateCookie(
        [FromBody] CreateCookieRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FactoryKey) || string.IsNullOrWhiteSpace(request.CookieType))
            return BadRequest(new { message = "FactoryKey and CookieType are required." });

        if (request.Price < 0 || request.Stock < 0)
            return BadRequest(new { message = "Price and stock must be zero or greater." });

        if (request.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId must be a positive integer." });

        try
        {
            var factory = CookieFactoryProvider.GetFactory(request.FactoryKey);
            var cookie = factory.CreateCookie(request.CookieType);

            cookie.Description = request.Description;
            cookie.Price = request.Price;
            cookie.Stock = request.Stock;
            cookie.CategoryId = request.CategoryId;

            var entity = new CookieEntity
            {
                Name = cookie.Name,
                Description = cookie.Description,
                Price = cookie.Price,
                Stock = cookie.Stock,
                CategoryId = cookie.CategoryId
            };

            var cookieId = await _cookieRepository.InsertAsync(entity, cancellationToken);
            cookie.CookieId = cookieId;

            return Created($"/api/products/{cookieId}", new
            {
                cookieId,
                cookie.Name,
                cookie.Description,
                cookie.Price,
                cookie.Stock,
                cookie.CategoryId,
                factoryKey = request.FactoryKey,
                cookieType = request.CookieType
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
