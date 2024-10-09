using GeospatialWeb.Components;
using GeospatialWeb.Services;

namespace GeospatialWeb;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services   = builder.Services;

        // Add services to the container
        {
            services.AddRazorComponents();

            services.AddHostedService<DbSeedService>();

            // builder.AddCosmosInfrastructure();

            // builder.AddMongoInfrastructure();

            // builder.AddEntityFrameworkInfrastructure();

            builder.AddRedisInfrastructure();
        }

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        {
            app.UseExceptionHandler("/Error");

            app.UseStaticFiles();

            app.UseAntiforgery();

            app.MapRazorComponents<App>();

            app.MapPoiEndpoints();
        }

        app.Run();
    }
}
