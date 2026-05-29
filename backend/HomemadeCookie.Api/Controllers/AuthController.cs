using System.Security.Claims;
using HomemadeCookie.Api.Models;
using HomemadeCookie.Api.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _users;

    public AuthController(UserRepository users)
    {
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var account = await _users.ValidateLoginAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (account is null)
            return Unauthorized(new { message = "Invalid email or password." });

        await SignInUserAsync(account);
        return Ok(ToProfile(account));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Ok(new { authenticated = false });

        var userId = GetUserId();
        if (userId is null)
            return Ok(new { authenticated = false });

        var account = await _users.GetByIdAsync(userId.Value, cancellationToken);
        if (account is null)
            return Ok(new { authenticated = false });

        return Ok(new { authenticated = true, user = ToProfile(account) });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
            return BadRequest(new { message = "Email is already registered." });

        var userId = await _users.RegisterCustomerAsync(request, cancellationToken);
        var account = await _users.GetByIdAsync(userId, cancellationToken);
        if (account is null)
            return StatusCode(500, new { message = "Registration failed." });

        await SignInUserAsync(account);
        return Created("/api/auth/me", ToProfile(account));
    }

    private async Task SignInUserAsync(UserAccount account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    private static object ToProfile(UserAccount account) => new
    {
        userId = account.UserId,
        name = account.Name,
        email = account.Email,
        role = account.Role,
        address = account.Address,
        phoneNumber = account.PhoneNumber
    };

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
