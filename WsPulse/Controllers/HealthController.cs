using Microsoft.AspNetCore.Mvc;

namespace WsPulse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    { _logger = logger; }

    [HttpGet("dependencies")]
    public IActionResult CheckDependencies()
    {
        _logger.LogInformation("Checking dependenceies...");
        // TODO
        // Trigger dependency healthchecks

        return this.Ok(new { message = "Dependency check executed." });
    }

    [HttpGet("aggregated")]
    public IActionResult AggregatedCheck()
    {
        _logger.LogInformation("Performing aggregated health check...");

        return this.Ok(new { message = "Aggregated check executed" });
    }
}
