using Microsoft.AspNetCore.Mvc;
using WsPulse.Models;

namespace WsPulse.Controllers;


[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;

    public StatusController(ILogger<StatusController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAllStatus()
    {
        _logger.LogInformation("Request all service status");
        // TODO
        // Return list from InMemory/Mongo

        return this.Ok(new List<ServiceInfo>());
    }

    [HttpGet("{name}")]
    public IActionResult GetServiceStatus(string name)
    {
        _logger.LogInformation("Requested status for {ServiceName}", name);
        // TODO
        // Return single service
        // or Return multiple services ?!?!?

        return this.Ok(new { service = name, StatusCode = "unknown" });
    }
}
