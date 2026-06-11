using HomemadeCookie.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HomemadeCookie.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var databaseConnected = await DatabaseConnection.Instance.PingAsync(cancellationToken);

            if (!databaseConnected)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unhealthy",
                    database = "disconnected"
                });
            }

            return Ok(new
            {
                status = "healthy",
                database = "connected"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                database = "error",
                message = ex.Message
            });
        }
    }
}
