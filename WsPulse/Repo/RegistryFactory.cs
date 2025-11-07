using MongoDB.Driver;
using WsPulse.Config;
using WsPulse.Interfaces;

namespace WsPulse.Repo;

public class RegistryFactory
{
    public static IServiceRegistry Create(EnvironmentSettings settings, ILoggerFactory loggerFactory)
    {
        if (!String.IsNullOrWhiteSpace(settings.MongoConnectionString))
        {
            var logger = loggerFactory.CreateLogger<MongoServiceRegistry>();
            var client = new MongoClient(settings.MongoConnectionString);
            logger.LogInformation("Using MongoDB as persistent registry (DB: {Database})", settings.MongoDatabase);
            return new MongoServiceRegistry(client, settings.MongoDatabase, logger);
        }
        else
        {
            var logger = loggerFactory.CreateLogger<InMemoryServiceRegistry>();
            logger.LogInformation("No MongoDB connection string found. Using in-memory registry.");
            return new InMemoryServiceRegistry(logger);
        }
    }
}
