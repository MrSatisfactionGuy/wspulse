
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WsPulse.Config;
using WsPulse.Enums;
using WsPulse.Interfaces;
using WsPulse.Repo;

namespace WsPulse;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Sources
        builder.Configuration.AddEnvironmentVariables();

        // Integrate Settings-Class
        builder.Services.Configure<EnvironmentSettings>(
            builder.Configuration.GetSection("EnvironmentSettings"));

        // Core ASP.NET Features (adding services to the container.)
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Swagger Configuration
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "WsPulse API",
                Version = "v1",
                Description = "Centralized health and dependency monitoring service for distributed WebServices."
            });
        });

        // Persistent Layer
        // Hier entscheidet WsPulse dynamisch, ob Mongo verwendet werden soll.
        // Wenn kein gültiger Mongo-ConnectionString gefunden wird, wrid automatisch
        // auf das InMemory-Repository zurückgegriffen
        builder.Services.AddSingleton<IServiceRegistry>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<EnvironmentSettings>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return RegistryFactory.Create(settings, loggerFactory);
        });

        // Build Application
        var app = builder.Build();

        // Configure HTTP Request Pipeline
        var settings = app.Services.GetRequiredService<IOptions<EnvironmentSettings>>().Value;

        // Swagger nur in nicht-produktiven Umgebungen aktivieren
        if (settings.PortainerEnvironment is not PortainerEnvironmentEnum.ProdExtern &&
            settings.PortainerEnvironment is not PortainerEnvironmentEnum.ProdIntern)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "WsPulse API v1");
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
            });
        }
        else
        {
            var log = app.Services.GetRequiredService<ILogger<Program>>();
            log.LogInformation("Swagger disabled for {Env} environment.", settings.PortainerEnvironment);
        }

        // Standard Middleware
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        // Startup diagnostics (environment + Mongo state)
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("[WsPulse] Startup Diagnostics:");
        logger.LogInformation(" • Environment: {Env}", settings.PortainerEnvironment);
        logger.LogInformation(" • Using Mongo: {MongoActive}",
            !String.IsNullOrWhiteSpace(settings.MongoConnectionString));

        app.Run();
    }
}
