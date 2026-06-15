using HomemadeCookie.Api.DTOs;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Patterns.Factory;
using HomemadeCookie.Api.Patterns.State;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
    private readonly ReviewRepository _reviewRepository;

    public AdminController(
        CookieRepository cookieRepository,
        OrderRepository orderRepository,
        CategoryRepository categoryRepository,
        UserRepository userRepository,
        ReviewRepository reviewRepository)
    {
        _cookieRepository = cookieRepository;
        _orderRepository = orderRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _reviewRepository = reviewRepository;
    }

    // =========================
    // ORDERS (With Server-Side Date Range Filtering)
    // =========================

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] DateTime? startDate, 
        [FromQuery] DateTime? endDate, 
        CancellationToken cancellationToken)
    {
        // 1. Fetch all orders from your Dapper repository layer
        var orders = await _orderRepository.GetAllAsync(cancellationToken);

        // 2. Perform conditional filtration directly on the collection before serialization
        if (startDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate >= startDate.Value).ToList();
        }

        if (endDate.HasValue)
        {
            // Set the limit explicitly to the end of the day (23:59:59) to prevent missing transactions
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            orders = orders.Where(o => o.OrderDate <= endOfDay).ToList();
        }

        return Ok(orders);
    }

    [HttpGet("orders/{id:int}/details")]
    public async Task<IActionResult> GetOrderDetails(int id, CancellationToken cancellationToken)
    {
        var details = await _orderRepository.GetAdminOrderDetailsAsync(id, cancellationToken);

        if (details is null)
            return NotFound(new { message = $"Order {id} not found." });

        return Ok(details);
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

    // =========================
    // COOKIES (CREATE WITH IMAGE UPLOAD)
    // =========================

    [HttpPost("cookies")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> CreateCookie(
        [FromForm] CreateCookieRequest request,
        [FromForm] IFormFile? image,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FactoryKey) ||
            string.IsNullOrWhiteSpace(request.CookieType))
        {
            return BadRequest(new { message = "FactoryKey and CookieType are required." });
        }

        if (request.Price < 0 || request.Stock < 0)
            return BadRequest(new { message = "Price and stock must be zero or greater." });

        if (request.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId must be a positive integer." });

        string? imageUrl = null;
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        if (image is not null && image.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadFolder, fileName);

            // Using structured scoping to flush streaming locks immediately
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream, cancellationToken);
            }

            imageUrl = $"/uploads/{fileName}";
        }

        try
        {
            var factory = CookieFactoryProvider.GetFactory(request.FactoryKey);
            var cookie = factory.CreateCookie(request.CookieType);

            cookie.Description = request.Description;
            cookie.Price = request.Price;
            cookie.Stock = request.Stock;
            cookie.CategoryId = request.CategoryId;
            cookie.ImageUrl = imageUrl;

            var entity = new CookieEntity
            {
                Name = cookie.Name,
                Description = cookie.Description,
                ImageUrl = cookie.ImageUrl,
                Price = cookie.Price,
                Stock = cookie.Stock,
                CategoryId = cookie.CategoryId,
                FactoryKey = request.FactoryKey,
                CookieType = request.CookieType
            };

            var cookieId = await _cookieRepository.InsertAsync(entity, cancellationToken);

            return Created($"/api/products/{cookieId}", new
            {
                cookieId,
                cookie.Name,
                cookie.Description,
                cookie.ImageUrl,
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

    // =========================
    // UPDATE COOKIE
    // =========================

    [HttpPut("cookies/{id:int}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UpdateCookie(
        int id,
        [FromForm] UpdateCookieRequest request,
        [FromForm] IFormFile? image,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Cookie name is required." });

        if (request.Price < 0 || request.Stock < 0)
            return BadRequest(new { message = "Price and stock must be zero or greater." });

        string? imageUrl = request.ImageUrl;

        if (image is not null && image.Length > 0)
        {
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream, cancellationToken);
            }

            imageUrl = $"/uploads/{fileName}";
        }

        request.ImageUrl = imageUrl;

        var updated = await _cookieRepository.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound(new { message = $"Cookie {id} not found." });
    }

    // =========================
    // DELETE COOKIE
    // =========================

    [HttpDelete("cookies/{id:int}")]
    public async Task<IActionResult> DeleteCookie(int id, CancellationToken cancellationToken)
    {
        var deleted = await _cookieRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = $"Cookie {id} not found." });
    }

    // =========================
    // CATEGORIES
    // =========================

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

    // =========================
    // REVIEWS
    // =========================
    
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews(CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetAllAsync(cancellationToken);
        return Ok(reviews);
    }

    // =========================
    // USERS
    // =========================

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

    // =========================
    // SALES
    // =========================

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesData(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var sales = await _orderRepository.GetCookieSalesAsync(startDate, endDate, cancellationToken);
        return Ok(sales);
    }
}