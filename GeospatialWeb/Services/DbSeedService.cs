namespace GeospatialWeb.Services;

public sealed record PoiSeedRecord(string Name, string Category, double Lng, double Lat);

public sealed class DbSeedService(IServiceScopeFactory _serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IServiceProvider serviceProvider = scope.ServiceProvider;

        IPoiService poiService = serviceProvider.GetRequiredService<IPoiService>();

        await poiService.DatabaseSeed(stoppingToken);
    }
}
