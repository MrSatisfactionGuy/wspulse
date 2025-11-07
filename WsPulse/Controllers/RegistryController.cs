using Microsoft.AspNetCore.Mvc;
using System.Net;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Controllers;


[ApiController]
[Route("api/[controller]")]
public class RegistryController : ControllerBase
{
    private readonly IServiceRegistry _registry;
    private readonly ILogger<RegistryController> _logger;

    public RegistryController(IServiceRegistry registry, ILogger<RegistryController> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Registriert oder aktualisiert einen Service in der zentralen WsPulse-Registry.
    /// </summary>
    [HttpPost("/api/register")]
    [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RegistryServiceAsync([FromBody] ServiceInfo service)
    {
        if (service == null || String.IsNullOrWhiteSpace(service.Name))
            return this.BadRequest("Invalid service registration data.");
        try
        {
            bool success = await _registry.RegisterServiceAsync(service);

            if (!success)
            {
                _logger.LogWarning("Failed to register service '{ServiceName}'", service.Name);

                return this.Problem("Service registration failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            _logger.LogInformation("Service '{ServiceName}' registered successfully", service.Name);

            return this.Ok(new { message = "Service registered successfully", service });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service registration for {ServiceName}", service?.Name);

            return this.Problem("Internal error during service registration.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
