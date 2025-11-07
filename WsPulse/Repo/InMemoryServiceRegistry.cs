using System.Collections.Concurrent;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Repo;

public class InMemoryServiceRegistry : IServiceRegistry
{

    private readonly ConcurrentDictionary<string, ServiceInfo> _services = new();
    private readonly ILogger<InMemoryServiceRegistry> _logger;

    public InMemoryServiceRegistry(ILogger<InMemoryServiceRegistry> logger)
    {
        _logger = logger;
    }

    public Task<ServiceInfo?> FindByNameAsync(string name)
    {
        _services.TryGetValue(name, out var service);
        return Task.FromResult(service);
    }

    public Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
    {
        IEnumerable<ServiceInfo> services = _services.Values;
        return Task.FromResult(services);
    }

    public Task<bool> IsRegisteredAsync(string name)
    {
        bool exists = _services.ContainsKey(name);
        return Task.FromResult(exists);
    }


    public Task<bool> RegisterServiceAsync(ServiceInfo service)
    {
        if (service == null || String.IsNullOrWhiteSpace(service.Name))
        {
            _logger.LogWarning("Invalid service registration attempt (missing name or null)");
            return Task.FromResult(false);
        }

        service.LastChecked = DateTime.UtcNow;

        _services.AddOrUpdate(service.Name, service, (_, _) => service);
        _logger.LogInformation("Registered or updated service {ServiceName}", service.Name);
        return Task.FromResult(true);

    }

    public Task<bool> UnregisterServiceAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name)) return Task.FromResult(false);

        bool removed = _services.TryRemove(name, out _);

        if (removed)
            _logger.LogInformation("Unregistered service {ServiceName}", name);
        else
            _logger.LogWarning("Attempted to unregister unknown service {ServiceName}", name);

        return Task.FromResult(removed);
    }
}
