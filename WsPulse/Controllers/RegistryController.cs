using Microsoft.AspNetCore.Mvc;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Controllers;


[ApiController]
[Route("api")]
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
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RegistryServiceAsync([FromBody] ServiceInfo service)
    {
        if (service is null)
            return this.BadRequest("Invalid service registration data.");

        if (String.IsNullOrWhiteSpace(service.Name))
            return this.BadRequest("Service.Name is required.");

        if (String.IsNullOrWhiteSpace(service.Url))
            return this.BadRequest("Service.Url is required.");

        service.Name = service.Name.Trim();
        service.Url = service.Url.Trim().TrimEnd('/');

        service.Dependencies = (service.Dependencies ?? new List<string>())
            .Where(d => !String.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        service.Environment = String.IsNullOrWhiteSpace(service.Environment)
            ? null
            : service.Environment.Trim();

        // Register = Upsert + Timestamp
        service.LastChecked = DateTime.UtcNow;

        try
        {
            bool success = await _registry.RegisterServiceAsync(service);

            if (!success)
            {
                _logger.LogWarning("Failed to register service '{ServiceName}'", service.Name);
                return this.Problem("Service registration failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            _logger.LogInformation("Service '{ServiceName}' registered/updated successfully", service.Name);
            return this.Ok(new { message = "Service registered successfully", service });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service registration for {ServiceName}", service.Name);
            return this.Problem("Internal error during service registration.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Entfernt einen Service aus der Registry.
    /// </summary>
    [HttpDelete("register/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterServiceAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
            return this.BadRequest("Service name is required.");

        var removed = await _registry.UnregisterServiceAsync(name);

        if (!removed)
            return this.NotFound(new ProblemDetails
            {
                Title = "Service not found",
                Detail = $"No service registered with name '{name}'.",
                Status = StatusCodes.Status404NotFound
            });

        _logger.LogInformation("Service '{ServiceName}' unregistered", name);
        return this.Ok(new { message = "Service unregistered successfully", name });
    }


    /// <summary>
    /// Liest einen registrierten Service aus der Registry.
    /// </summary>
    [HttpGet("register/{name}")]
    [ProducesResponseType(typeof(ServiceInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegisteredServiceAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
            return this.BadRequest("Service name is required.");

        var service = await _registry.FindByNameAsync(name);

        if (service is null)
            return this.NotFound(new ProblemDetails
            {
                Title = "Service not found",
                Detail = $"No service registered with name '{name}'.",
                Status = StatusCodes.Status404NotFound
            });

        return this.Ok(service);
    }
}
