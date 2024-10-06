using GeospatialWeb.Services.Redis;
using StackExchange.Redis;

namespace GeospatialWeb.Services;

public static class InfrastructureExtensionsRedis
{
    public static void AddRedisInfrastructure(this IHostApplicationBuilder builder)
    {
        IServiceCollection services = builder.Services;

        services.AddSingleton<IPoiService, PoiServiceRedis>();

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            string connectionString = builder.Configuration.GetConnectionString("Redis")
                ?? throw new NullReferenceException("Missing configuration Redis connection string");

            return ConnectionMultiplexer.Connect(connectionString);
        });
    }
}
