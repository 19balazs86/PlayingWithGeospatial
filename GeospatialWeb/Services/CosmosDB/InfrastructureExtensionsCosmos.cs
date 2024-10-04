using GeospatialWeb.Services.CosmosDB;
using Microsoft.Azure.Cosmos;

namespace GeospatialWeb.Services;

public static class InfrastructureExtensionsCosmos
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
