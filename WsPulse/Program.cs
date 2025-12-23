
using Microsoft.Extensions.Options;
using WsPulse.Config;
using WsPulse.Enums;
using WsPulse.HostedServices;
using WsPulse.Interfaces;
using WsPulse.Repo;

namespace WsPulse;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configure Sources
        builder.Configuration.AddEnvironmentVariables();

        // Integrate Settings-Class
        builder.Services.Configure<EnvironmentSettings>(
            builder.Configuration.GetSection("EnvironmentSettings"));

        builder.Services.Configure<PollingConfig>(
            builder.Configuration.GetSection("Polling"));

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
            EnvironmentSettings settings = sp.GetRequiredService<IOptions<EnvironmentSettings>>().Value;
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return RegistryFactory.Create(settings, loggerFactory);
        });

        builder.Services.AddSingleton<IDependencyRegistry, InMemoryDependencyRegistry>();

        builder.Services.AddHttpClient("Polling", (sp, client) =>
        {
            PollingConfig polling = sp.GetRequiredService<IOptions<PollingConfig>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, polling.TimeoutSeconds));
        });

        builder.Services.AddHostedService<ServicePollingHostedService>();

        // Build Application
        WebApplication app = builder.Build();

        // Configure HTTP Request Pipeline
        EnvironmentSettings settings = app.Services.GetRequiredService<IOptions<EnvironmentSettings>>().Value;

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
            ILogger<Program> log = app.Services.GetRequiredService<ILogger<Program>>();
            log.LogInformation("Swagger disabled for {Env} environment.", settings.PortainerEnvironment);
        }

        // Standard Middleware
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        // Startup diagnostics (environment + Mongo state)
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("[WsPulse] Startup Diagnostics:");
        logger.LogInformation(" • Environment: {Env}", settings.PortainerEnvironment);
        logger.LogInformation(" • Using Mongo: {MongoActive}",
            !String.IsNullOrWhiteSpace(settings.MongoConnectionString));

        app.Run();
    }
}
