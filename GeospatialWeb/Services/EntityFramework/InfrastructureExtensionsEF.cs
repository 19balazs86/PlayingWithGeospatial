using GeospatialWeb.Services.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace GeospatialWeb.Services;

public static class InfrastructureExtensionsEF
{
    public static void AddEntityFrameworkInfrastructure(this IHostApplicationBuilder builder)
    {
        IServiceCollection services = builder.Services;

        services.AddScoped<IPoiService, PoiServiceEF>();

        // For better performance use AddDbContextPool instead of AddDbContext
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
                ?? throw new NullReferenceException("Missing ConnectionString: 'PostgreSQL'");

            options.UseNpgsql(connectionString, dbOptions => dbOptions.UseNetTopologySuite());

            // optionsBuilder.UseInMemoryDatabase("MyDatabase").UseNetTopologySuite(); // With package: NetTopologySuite

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }
}
