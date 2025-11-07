
using MongoDB.Driver;
using WsPulse.Interfaces;
using WsPulse.Repo;

namespace WsPulse;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Persistent Layer
        // Condition for MongoDB
        //builder.Services.AddSingleton<IMongoClient>
        //    (sp => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));
        //builder.Services.AddSingleton<IServiceRegistry, MongoServiceRegistry>();

        // Use InMemory registry by default
        builder.Services.AddSingleton<IServiceRegistry, InMemoryServiceRegistry>();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
