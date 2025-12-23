using System.Collections.Concurrent;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Repo;

public class InMemoryServiceRegistry : IServiceRegistry
{
    // Wichtig: Case-insensitive Keying
    private readonly ConcurrentDictionary<string, ServiceInfo> _services =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<InMemoryServiceRegistry> _logger;

    public InMemoryServiceRegistry(string databaseName, ILogger<InMemoryServiceRegistry> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initialized InMemory registry (logical DB name: {Database})", databaseName);
    }

    public InMemoryServiceRegistry(ILogger<InMemoryServiceRegistry> logger)
        : this("WsPulse", logger)
    {
    }

    public Task<ServiceInfo?> FindByNameAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
            return Task.FromResult<ServiceInfo?>(null);

        _services.TryGetValue(name.Trim(), out var service);
        return Task.FromResult(service);
    }

    public Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
    {
        return Task.FromResult<IEnumerable<ServiceInfo>>(_services.Values);
    }

    public Task<bool> IsRegisteredAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        return Task.FromResult(_services.ContainsKey(name.Trim()));
    }

    public Task<bool> RegisterServiceAsync(ServiceInfo service)
    {
        if (service is null || String.IsNullOrWhiteSpace(service.Name))
        {
            _logger.LogWarning("Invalid service registration attempt (missing name or null)");
            return Task.FromResult(false);
        }

        // Controller setzt LastChecked bereits; falls RegisterServiceAsync direkt genutzt wird:
        if (service.LastChecked == default)
            service.LastChecked = DateTime.UtcNow;

        string key = service.Name.Trim();

        _services.AddOrUpdate(
            key,
            // Insert: einfach speichern (neues Objekt)
            _ => service,
            // Update: merge, Polling-Status behalten
            (_, existing) =>
            {
                existing.Url = service.Url;
                existing.Dependencies = service.Dependencies;
                existing.Environment = service.Environment;
                existing.LastChecked = service.LastChecked;

                // IsReachable NICHT überschreiben (Polling ist Source of Truth)
                return existing;
            });

        _logger.LogInformation("Registered or updated service {ServiceName}", key);
        return Task.FromResult(true);
    }

    public Task<bool> UnregisterServiceAsync(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        bool removed = _services.TryRemove(name.Trim(), out _);

        if (removed)
            _logger.LogInformation("Unregistered service {ServiceName}", name);
        else
            _logger.LogWarning("Attempted to unregister unknown service {ServiceName}", name);

        return Task.FromResult(removed);
    }

    public Task<bool> UpdateStatusAsync(string name, bool isReachable, DateTime lastCheckedUtc)
    {
        if (String.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        string key = name.Trim();

        if (!_services.TryGetValue(key, out var existing))
            return Task.FromResult(false);

        existing.IsReachable = isReachable;
        existing.LastChecked = lastCheckedUtc;

        return Task.FromResult(true);
    }

    public Task<bool> UpdateOperationalAsync(string name, bool isOperational, DateTime lastCheckedUtc)
    {
        if (String.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        if (!_services.TryGetValue(name.Trim(), out var existing))
            return Task.FromResult(false);

        existing.IsOperational = isOperational;
        existing.LastChecked = lastCheckedUtc;
        return Task.FromResult(true);
    }
}
