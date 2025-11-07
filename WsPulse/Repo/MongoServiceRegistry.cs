using MongoDB.Driver;
using WsPulse.Interfaces;
using WsPulse.Models;

namespace WsPulse.Repo;

public class MongoServiceRegistry : IServiceRegistry
{
    private readonly IMongoCollection<ServiceInfo> _collection;
    private readonly ILogger<MongoServiceRegistry> _logger;

    public MongoServiceRegistry(IMongoClient client, ILogger<MongoServiceRegistry> logger)
    {
        _logger = logger;
        var db = client.GetDatabase("WsPulse");
        _collection = db.GetCollection<ServiceInfo>("services");
    }

    public ServiceInfo? FindByName(string name)
    {
        return _collection.Find(s => s.Name == name).FirstOrDefault();
    }

    public IEnumerable<ServiceInfo> GetAllServices()
    {
        return _collection.Find(FilterDefinition<ServiceInfo>.Empty).ToList();
    }

    public bool IsRegistered(string name)
    {
        return _collection.Find(s => s.Name == name).Any();
    }

    public bool RegisterService(ServiceInfo service)
    {
        try
        {
            var filter = Builders<ServiceInfo>.Filter.Eq(s => s.Name, service.Name);
            _collection.ReplaceOne(filter, service, new ReplaceOptions { IsUpsert = true });
            _logger.LogInformation("Registered or updated {ServiceName} in MongoDB", service.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering {ServiceName} in MongoDB", service.Name);
            return false;
        }
    }

    public bool UnregisterService(string name)
    {
        var result = _collection.DeleteOne(s => s.Name == name);
        return result.DeletedCount > 0;
    }
}
