using WsPulse.Models;

namespace WsPulse.Interfaces;

public interface IServiceRegistry
{
    Task<bool> RegisterServiceAsync(ServiceInfo service);
    Task<bool> UnregisterServiceAsync(string name);
    Task<ServiceInfo?> FindByNameAsync(string name);
    Task<IEnumerable<ServiceInfo>> GetAllServicesAsync();
    Task<bool> IsRegisteredAsync(string name);
}