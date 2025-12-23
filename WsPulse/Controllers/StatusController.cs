using Microsoft.AspNetCore.Mvc;
using WsPulse.Interfaces;
using WsPulse.Models;
using WsPulse.Models.Dtos;

namespace WsPulse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly IDependencyRegistry _dependencyRegistry;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IServiceRegistry serviceRegistry, IDependencyRegistry dependencyRegistry, ILogger<StatusController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _dependencyRegistry = dependencyRegistry;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStatus()
    {
        _logger.LogInformation("Request all service status");

        IEnumerable<ServiceInfo> services = await _serviceRegistry.GetAllServicesAsync();
        Dictionary<string, DependencyInfo> depMap = await this.BuildDependencyMapAsync();

        List<ServiceStatusDto> result = services.Select(s => ToDto(s, depMap)).ToList();
        return this.Ok(result);
    }

    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ServiceStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceStatus(string name)
    {
        if (String.IsNullOrWhiteSpace(name)) return this.BadRequest("Service name is required.");

        // damit name%20 zu name wird
        name = name.Trim();

        _logger.LogInformation("Requested status for {ServiceName}", name);

        ServiceInfo? service = await _serviceRegistry.FindByNameAsync(name);

        if (service is null)
            return this.NotFound(new ProblemDetails
            {
                Title = "Service not found",
                Detail = $"No service registered with name '{name}'.",
                Status = StatusCodes.Status404NotFound
            });

        Dictionary<string, DependencyInfo> depMap = await this.BuildDependencyMapAsync();
        ServiceStatusDto dto = ToDto(service, depMap);
        return this.Ok(dto);
    }

    private async Task<Dictionary<string, DependencyInfo>> BuildDependencyMapAsync()
    {
        IEnumerable<DependencyInfo> deps = await _dependencyRegistry.GetAllAsync();
        return deps.ToDictionary(d => d.BaseUrl.TrimEnd('/'), StringComparer.OrdinalIgnoreCase);
    }

    private static ServiceStatusDto ToDto(ServiceInfo s, Dictionary<string, DependencyInfo> depMap)
    {
        ServiceStatusDto dto = new ServiceStatusDto
        {
            Name = s.Name,
            Url = s.Url,
            Environment = s.Environment,
            IsReachable = s.IsReachable,
            IsOperational = s.IsOperational,
            LastChecked = s.LastChecked
        };

        //dieser dicke Ausdruck ist für Sicherung von "" und " " da (wtf)
        foreach (string dep in (s.Dependencies ?? new List<string>()).Where(x => !String.IsNullOrWhiteSpace(x)))
        {
            string key = dep.Trim().TrimEnd('/');
            if (depMap.TryGetValue(key, out var d))
            {
                dto.Dependencies.Add(new DependencyStatusDto
                {
                    BaseUrl = d.BaseUrl,
                    IsReachable = d.IsReachable,
                    LastChecked = d.LastChecked
                });
            }
            else
            {
                // Dependency bekannt (im Service eingetragen), aber (noch) nicht im Registry-Cache gepollt
                dto.Dependencies.Add(new DependencyStatusDto
                {
                    BaseUrl = key,
                    IsReachable = false,
                    LastChecked = default
                });
            }
        }

        return dto;
    }
}
