using MongoDB.Driver;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Repo;

public class MongoServiceRegistry : IServiceRegistry
{
    private readonly IMongoCollection<ServiceInfo> _collection;
    private readonly ILogger<MongoServiceRegistry> _logger;

    public MongoServiceRegistry(IMongoClient client, string databaseName, ILogger<MongoServiceRegistry> logger)
    {
        _logger = logger;
        var db = client.GetDatabase("WsPulse");
        _collection = db.GetCollection<ServiceInfo>("services");

        _logger.LogInformation("Connected to MongoDB database: {DatabaseName}", databaseName);
    }

    public MongoServiceRegistry(IMongoClient client, ILogger<MongoServiceRegistry> logger)
        : this(client, "WsPulse", logger)
    {
    }

    public async Task<ServiceInfo?> FindByNameAsync(string name)
    {
        return await _collection.Find(s => s.Name == name).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
    {
        var services = await _collection.Find(FilterDefinition<ServiceInfo>.Empty).ToListAsync();
        return services;
    }

    public async Task<bool> IsRegisteredAsync(string name)
    {
        var count = await _collection.CountDocumentsAsync(s => s.Name == name);
        return count > 0;
    }

    public async Task<bool> RegisterServiceAsync(ServiceInfo service)
    {
        try
        {
            if (service == null || String.IsNullOrWhiteSpace(service.Name))
            {
                _logger.LogWarning("Invalid service registration attempt (missing name or null)");
                return false;
            }

            var filter = Builders<ServiceInfo>.Filter.Eq(s => s.Name, service.Name);
            await _collection.ReplaceOneAsync(filter, service, new ReplaceOptions { IsUpsert = true });

            _logger.LogInformation("Registered or updated {ServiceName} in MongoDB", service.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering {ServiceName} in MongoDB", service?.Name);
            return false;
        }
    }

    public async Task<bool> UnregisterServiceAsync(string name)
    {
        try
        {
            var result = await _collection.DeleteOneAsync(s => s.Name == name);

            if (result.DeletedCount > 0)
            {
                _logger.LogInformation("Unregistered service {ServiceName} from MongoDB", name);
                return true;
            }

            _logger.LogWarning("Attempted to unregister unknown service {ServiceName}", name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering {ServiceName} from MongoDB", name);
            return false;
        }
    }
}
