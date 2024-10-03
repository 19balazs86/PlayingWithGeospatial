using Microsoft.Azure.Cosmos;

namespace GeospatialWeb.Services.CosmosDB;

public static class InfrastructureExtensions
{
    public static void AddCosmosInfrastructure(this IHostApplicationBuilder builder)
    {
        IServiceCollection services = builder.Services;

        services.AddSingleton<IPoiService, PoiServiceCosmos>();

        services.AddSingleton(serviceProvider =>
        {
            string connectionString = builder.Configuration.GetConnectionString("CosmosDB")
                ?? throw new NullReferenceException("Missing configuration CosmosDB connection string");

            return new CosmosClient(connectionString);
        });
    }
}
