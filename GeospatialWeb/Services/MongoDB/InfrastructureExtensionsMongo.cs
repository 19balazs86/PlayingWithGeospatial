using GeospatialWeb.Services.MongoDB;
using MongoDB.Driver;

namespace GeospatialWeb.Services;

public static class InfrastructureExtensionsMongo
{
    public static void AddMongoInfrastructure(this IHostApplicationBuilder builder)
    {
        IServiceCollection services = builder.Services;

        services.AddSingleton<IPoiService, PoiServiceMongo>();

        services.AddSingleton(serviceProvider =>
        {
            string connectionString = builder.Configuration.GetConnectionString("MongoDB")
                ?? throw new NullReferenceException("Missing configuration MongoDB connection string");

            return new MongoClient(connectionString);
        });
    }
}
