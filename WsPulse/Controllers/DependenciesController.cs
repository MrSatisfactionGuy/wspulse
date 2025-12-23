using Microsoft.AspNetCore.Mvc;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Controllers;

[ApiController]
[Route("api/dependencies")]
public class DependenciesController : ControllerBase
{
    private readonly IDependencyRegistry _deps;
    private readonly IServiceRegistry _services;

    public DependenciesController(IDependencyRegistry deps, IServiceRegistry services)
    {
        _deps = deps;
        _services = services;
    }

    // GET /api/dependencies/status
    [HttpGet("status")]
    [ProducesResponseType(typeof(IEnumerable<DependencyInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDependencyStatus()
    {
        IEnumerable<DependencyInfo> all = await _deps.GetAllAsync();
        return this.Ok(all);
    }

    // GET /api/dependencies/impact?baseUrl=https://test.api.auth.inpro-electric.de
    [HttpGet("impact")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetImpact([FromQuery] string baseUrl)
    {
        if (String.IsNullOrWhiteSpace(baseUrl))
            return this.BadRequest("Query parameter 'baseUrl' is required.");

        string key = baseUrl.Trim().TrimEnd('/');

        List<ServiceInfo> services = (await _services.GetAllServicesAsync()).ToList();

        List<string> affected = services
            .Where(s => (s.Dependencies ?? new List<string>())
            .Any(d => String.Equals(d.Trim().TrimEnd('/'), key, StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.Name)
            .OrderBy(x => x)
            .ToList();

        return this.Ok(affected);
    }
}
