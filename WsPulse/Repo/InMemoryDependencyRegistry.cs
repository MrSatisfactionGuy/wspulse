using System.Collections.Concurrent;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Repo;

public class InMemoryDependencyRegistry : IDependencyRegistry
{
    private readonly ConcurrentDictionary<string, DependencyInfo> _deps =
      new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(DependencyInfo dependency)
    {
        if (dependency is null || String.IsNullOrWhiteSpace(dependency.BaseUrl))
            return Task.CompletedTask;

        string key = dependency.BaseUrl.Trim().TrimEnd('/');

        _deps.AddOrUpdate(
            key,
            _ => new DependencyInfo
            {
                BaseUrl = key,
                IsReachable = dependency.IsReachable,
                LastChecked = dependency.LastChecked
            },
            (_, existing) =>
            {
                existing.IsReachable = dependency.IsReachable;
                existing.LastChecked = dependency.LastChecked;
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task<DependencyInfo?> FindAsync(string baseUrl)
    {
        if (String.IsNullOrWhiteSpace(baseUrl))
            return Task.FromResult<DependencyInfo?>(null);

        _deps.TryGetValue(baseUrl.Trim().TrimEnd('/'), out var dep);
        return Task.FromResult(dep);
    }

    public Task<IEnumerable<DependencyInfo>> GetAllAsync()
        => Task.FromResult<IEnumerable<DependencyInfo>>(_deps.Values);
}
