using GeospatialWeb.Services.MongoDB;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace GeospatialWeb.Services;

public static class InfrastructureExtensionsMongo
{
    public static void AddMongoInfrastructure(this IHostApplicationBuilder builder)
    {
        ConventionRegistry.Register("Conventions", new MongoDbConventions(), _ => true);

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
