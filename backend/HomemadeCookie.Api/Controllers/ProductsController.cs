using HomemadeCookie.Api.Repositories;
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
}
