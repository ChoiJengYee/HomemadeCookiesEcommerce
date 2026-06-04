using HomemadeCookie.Api.Repositories;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly CookieRepository _cookieRepository;

    public ProductsController(CookieRepository cookieRepository)
    {
        _cookieRepository = cookieRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var cookies = await _cookieRepository.GetAllAsync(cancellationToken);
        return Ok(cookies);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var cookie = await _cookieRepository.GetByIdAsync(id, cancellationToken);
        if (cookie is null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(cookie);
    }

[HttpPost]
[RequestSizeLimit(10_000_000)]
public async Task<IActionResult> Create(
    [FromForm] CreateCookieRequest request,
    [FromForm] IFormFile? image,
    CancellationToken cancellationToken)
{
    string? imageUrl = null;

    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    if (!Directory.Exists(uploadFolder))
        Directory.CreateDirectory(uploadFolder);

    if (image is not null && image.Length > 0)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream, cancellationToken);

        imageUrl = $"/uploads/{fileName}";
    }

    var cookie = new CookieEntity
    {
        FactoryKey = request.FactoryKey,
        CookieType = request.CookieType,
        Description = request.Description,
        Price = request.Price,
        Stock = request.Stock,
        CategoryId = request.CategoryId,
        ImageUrl = imageUrl
    };

    var id = await _cookieRepository.InsertAsync(cookie, cancellationToken);

    return Ok(new
    {
        cookieId = id,
        imageUrl
    });
}

}
