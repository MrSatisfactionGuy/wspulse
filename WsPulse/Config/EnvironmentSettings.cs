using WsPulse.Enums;

namespace WsPulse.Config;

/// <summary>
/// Enthält Umgebungsinformationen und MongoDB-Verbindungsdetails für WsPulse.
/// Wird automatisch über Environment-Variablen oder appsettings konfiguriert.
/// </summary>
public class EnvironmentSettings
{
    public PortainerEnvironmentEnum PortainerEnvironment { get; set; } = PortainerEnvironmentEnum.None;

    public bool IsProduction =>
        this.PortainerEnvironment is PortainerEnvironmentEnum.ProdExtern or PortainerEnvironmentEnum.ProdIntern;

    public bool IsTest =>
        this.PortainerEnvironment is PortainerEnvironmentEnum.TestExtern or PortainerEnvironmentEnum.TestIntern;

    /// <summary>
    /// Vollständiger Connection String zur MongoDB (z. B. "mongodb://user:pass@mongo:27017")
    /// </summary>
    public string MongoConnectionString { get; set; } = String.Empty;

    /// <summary>
    /// Name der zu verwendenden Mongo-Datenbank (Standard: "WsPulse")
    /// </summary>
    public string MongoDatabase { get; set; } = "WsPulse";

    /// <summary>
    /// Liest alle relevanten Einstellungen aus den Umgebungsvariablen.
    /// </summary>
    public void LoadFromEnvironment()
    {
        try
        {
            string? env = Environment.GetEnvironmentVariable("PortainerEnvironment");
            this.MongoConnectionString = Environment.GetEnvironmentVariable("MongoConnectionString") ?? String.Empty;
            this.MongoDatabase = Environment.GetEnvironmentVariable("MongoDatabase") ?? "WsPulse";

            this.PortainerEnvironment = env switch
            {
                "ProdExtern" => PortainerEnvironmentEnum.ProdExtern,
                "ProdIntern" => PortainerEnvironmentEnum.ProdIntern,
                "TestExtern" => PortainerEnvironmentEnum.TestExtern,
                "TestIntern" => PortainerEnvironmentEnum.TestIntern,
                _ => PortainerEnvironmentEnum.None
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WsPulse] Failed to load environment variables: {ex.Message}");
        }
    }

    /// <summary>
    /// Prüft, ob Mongo korrekt konfiguriert ist.
    /// </summary>
    public bool ValidateMongoConfig(ILogger logger)
    {
        if (String.IsNullOrWhiteSpace(this.MongoConnectionString))
        {
            logger.LogWarning("No MongoDB connection string found. Falling back to InMemory registry.");
            return false;
        }

        logger.LogInformation("MongoDB connection configured: Database={Database}", this.MongoDatabase);
        return true;
    }

}