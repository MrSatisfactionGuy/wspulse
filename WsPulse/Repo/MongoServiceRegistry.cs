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
        IMongoDatabase db = client.GetDatabase("WsPulse");
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
        List<ServiceInfo> services = await _collection.Find(FilterDefinition<ServiceInfo>.Empty).ToListAsync();
        return services;
    }

    public async Task<bool> IsRegisteredAsync(string name)
    {
        long count = await _collection.CountDocumentsAsync(s => s.Name == name);
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

            FilterDefinition<ServiceInfo> filter = Builders<ServiceInfo>.Filter.Eq(s => s.Name, service.Name);
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
            DeleteResult result = await _collection.DeleteOneAsync(s => s.Name == name);

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

    public async Task<bool> UpdateStatusAsync(string name, bool isReachable, DateTime lastCheckedUtc)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            FilterDefinition<ServiceInfo> filter = Builders<ServiceInfo>.Filter.Eq(s => s.Name, name.Trim());

            UpdateDefinition<ServiceInfo> update = Builders<ServiceInfo>.Update
                .Set(s => s.IsReachable, isReachable)
                .Set(s => s.LastChecked, lastCheckedUtc);

            UpdateResult result = await _collection.UpdateOneAsync(filter, update);

            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for {ServiceName} in MongoDB", name);
            return false;
        }
    }

    public async Task<bool> UpdateOperationalAsync(string name, bool isOperational, DateTime lastCheckedUtc)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            FilterDefinition<ServiceInfo> filter =
                Builders<ServiceInfo>.Filter.Eq(s => s.Name.ToLower(), name.Trim().ToLower());

            UpdateDefinition<ServiceInfo> update = Builders<ServiceInfo>.Update
                .Set(s => s.IsOperational, isOperational)
                .Set(s => s.LastChecked, lastCheckedUtc);

            UpdateResult result = await _collection.UpdateOneAsync(filter, update);

            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operational state for {ServiceName} in MongoDB", name);
            return false;
        }
    }
}
