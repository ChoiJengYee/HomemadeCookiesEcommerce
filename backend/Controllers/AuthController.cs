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
        var user = await _users.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { user = new { user.UserId, user.Name, user.Email, user.Role, user.Address, user.PhoneNumber } });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordDirect([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // 1. Validate incoming JSON properties
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and new password fields are required." });
        }

        // 2. Check if user email account actually exists
        if (!await _users.EmailExistsAsync(request.Email.Trim(), cancellationToken))
        {
            return BadRequest(new { message = "An account with this email address was not found." });
        }

        // 3. Execute database query to overwrite database record data directly
        var isUpdated = await _users.ResetPasswordDirectAsync(request.Email, request.Password, cancellationToken);
        
        if (!isUpdated)
        {
            return StatusCode(500, new { message = "Failed to update database record." });
        }

        return Ok(new { message = "Password updated successfully!" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Name, email and password are required." });

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
            return Conflict(new { message = "An account with this email already exists." });

        var user = await _users.RegisterAsync(request.Name.Trim(), request.Email.Trim(), request.Password, request.Address.Trim(), request.PhoneNumber.Trim(), cancellationToken);
        if (user is null)
            return Conflict(new { message = "Could not create account." });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { user = new { user.UserId, user.Name, user.Email, user.Role, user.Address, user.PhoneNumber } });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        if (!await _users.EmailExistsAsync(request.Email.Trim(), cancellationToken))
            return Ok(new { message = "If this email exists, a reset link has been sent." });

        // This example app does not send real email; implement email delivery in a production app.
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Ok(new { authenticated = false, user = (object?)null });

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Ok(new { authenticated = false, user = (object?)null });

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null) return Ok(new { authenticated = false, user = (object?)null });

        return Ok(new { authenticated = true, user = new { user.UserId, user.Name, user.Email, user.Role, user.Address, user.PhoneNumber } });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "Name, address and phone number are required." });

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized(new { message = "Unable to identify the current user." });

        var updated = await _users.UpdateProfileAsync(userId, request.Name.Trim(), request.Address.Trim(), request.PhoneNumber.Trim(), cancellationToken);
        if (updated is null)
            return NotFound(new { message = "User profile could not be updated." });

        return Ok(new { user = new { updated.UserId, updated.Name, updated.Email, updated.Role, updated.Address, updated.PhoneNumber } });
    }
}
