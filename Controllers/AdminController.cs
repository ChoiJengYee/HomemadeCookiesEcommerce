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
    private readonly CategoryRepository _categoryRepository;
    private readonly UserRepository _userRepository;

    public AdminController(
        CookieRepository cookieRepository,
        OrderRepository orderRepository,
        CategoryRepository categoryRepository,
        UserRepository userRepository)
    {
        _cookieRepository = cookieRepository;
        _orderRepository = orderRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
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

    [HttpPut("cookies/{id:int}")]
    public async Task<IActionResult> UpdateCookie(
        int id,
        [FromBody] UpdateCookieRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Cookie name is required." });

        if (request.Price < 0 || request.Stock < 0)
            return BadRequest(new { message = "Price and stock must be zero or greater." });

        if (request.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId must be a positive integer." });

        var updated = await _cookieRepository.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound(new { message = $"Cookie {id} not found." });
    }

    [HttpDelete("cookies/{id:int}")]
    public async Task<IActionResult> DeleteCookie(int id, CancellationToken cancellationToken)
    {
        var deleted = await _cookieRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = $"Cookie {id} not found." });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Category name is required." });

        var categoryId = await _categoryRepository.InsertAsync(request.Name, cancellationToken);
        return Created($"/api/admin/categories/{categoryId}", new { categoryId, name = request.Name });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Category name is required." });

        var updated = await _categoryRepository.UpdateAsync(id, request.Name, cancellationToken);
        return updated ? NoContent() : NotFound(new { message = $"Category {id} not found." });
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        var deleted = await _categoryRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = $"Category {id} not found." });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(
        int id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(new { message = "Role is required." });

        if (request.Role is not ("Admin" or "Customer"))
            return BadRequest(new { message = "Role must be Admin or Customer." });

        var updated = await _userRepository.UpdateRoleAsync(id, request.Role, cancellationToken);
        return updated ? NoContent() : NotFound(new { message = $"User {id} not found." });
    }
}
