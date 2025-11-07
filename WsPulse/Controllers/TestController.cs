using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WsPulse.Controllers;


[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> testLogger)
    { _logger = testLogger; }

    [HttpGet]
    [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        try
        {
            bool isOperational = true;

            if (!isOperational)
            {
                _logger.LogWarning("WsPulse is not operational at {Time}", DateTime.UtcNow);

                return this.Problem("WsPulse is not operational.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            _logger.LogDebug("WsPulse test successful at {Time}", DateTimeOffset.UtcNow);
            return this.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WsPulse /api/test check");

            return this.Problem("Internal error during health check", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
