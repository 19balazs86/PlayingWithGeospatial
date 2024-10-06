using GeospatialWeb.Geography;
using GeospatialWeb.Services.Redis.Models;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.Redis;

public sealed class PoiServiceRedis(IConnectionMultiplexer _connectionMultiplexer) : IPoiService
{
    // Redis does not support filtering by a point within a polygon
    private readonly Dictionary<string, GeoPolygon> _countryPolygons = [];

    private const int _dbNumber = 0;

    private readonly IDatabase _database = _connectionMultiplexer.GetDatabase(_dbNumber);

    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lat, poiRequest.Lng, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        string geoPoisKey = PoiData.GetGeoIdKey(countryName);

        GeoRadiusResult[] results = await _database.GeoRadiusAsync(
            geoPoisKey, poiRequest.Lng, poiRequest.Lat, poiRequest.Distance, GeoUnit.Meters, options: GeoRadiusOptions.WithDistance);

        foreach (var result in results)
        {
            string poiKey = PoiData.GetIdKey(countryName, result.Member!);

            string? poiJson = await _database.StringGetAsync(poiKey);

            if (string.IsNullOrEmpty(poiJson))
            {
                throw new NullReferenceException($"Poi({poiKey}) does not exists");
            }

            PoiData? poiData = JsonSerializer.Deserialize<PoiData?>(poiJson);

            // GeoPosition? geoPosition = result.Position.Value; // GeoRadiusOptions.WithCoordinates

            yield return new PoiResponse(poiData!.Id, poiData.Name, poiData.Category, poiData.Location.Lat, poiData.Location.Lng, result.Distance.GetValueOrDefault(-1));
        }
    }

    public Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default)
    {
        var geoLocation = new GeoLocation(latitude, longitude);

        foreach (var item in _countryPolygons)
        {
            if (GeoUtils.IsPointInPolygon(geoLocation, item.Value))
            {
                return Task.FromResult<string?>(item.Key);
            }
        }

        return Task.FromResult<string?>(string.Empty);
    }

    public async Task DatabaseSeed(CancellationToken ct = default)
    {
        await inicializeCountryPolygons();

        bool isDatabaseExists = _countryPolygons.Count > 0;

        if (isDatabaseExists)
        {
            return;
        }

        await databaseSeed_Country("France",  ct);
        await databaseSeed_Country("Spain",   ct);
        await databaseSeed_Country("Ireland", ct);

        await databaseSeed_POIs("France",  ct);
        await databaseSeed_POIs("Spain",   ct);
        await databaseSeed_POIs("Ireland", ct);
    }

    private async Task databaseSeed_Country(string countryName, CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Country.json"));

        CountrySeedRecord[]? seedRecords = await JsonSerializer.DeserializeAsync<CountrySeedRecord[]?>(fileStream, cancellationToken: ct);

        var geoFence_Coordinates = seedRecords!.Select(c => new GeoLocation(c.Lat, c.Lng)).ToList();

        var geoFence = new GeoPolygon(geoFence_Coordinates);

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = geoFence
        };

        _countryPolygons[countryName] = geoFence;

        string countryJson = JsonSerializer.Serialize(country, CountrySerializationContext.Default.Country);

        await _database.StringSetAsync(country.GetIdKey(), countryJson);
    }

    private async Task databaseSeed_POIs(string countryName, CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Poi.json"));

        var asyncEnum = JsonSerializer.DeserializeAsyncEnumerable<PoiSeedRecord?>(fileStream, cancellationToken: ct);

        IBatch batch = _database.CreateBatch();

        List<Task> tasks = [];

        await foreach (PoiSeedRecord? poiSeedRecord in asyncEnum)
        {
            var poiData = new PoiData
            {
                Id       = Guid.NewGuid(),
                Category = poiSeedRecord!.Category,
                Name     = poiSeedRecord.Name,
                Location = new GeoLocation(poiSeedRecord.Lat, poiSeedRecord.Lng)
            };

            Task task = batch.GeoAddAsync(PoiData.GetGeoIdKey(countryName), poiSeedRecord.Lng, poiSeedRecord.Lat, poiData.Id.ToString());

            tasks.Add(task);

            string poiDataJson = JsonSerializer.Serialize(poiData, PoiDataSerializationContext.Default.PoiData);

            task = batch.StringSetAsync(poiData.GetIdKey(countryName), poiDataJson);

            tasks.Add(task);
        }

        batch.Execute();

        await Task.WhenAll(tasks);
    }

    private async Task inicializeCountryPolygons()
    {
        string pattern = $"{Country.CollectionName}:*";

        IServer server = _connectionMultiplexer.GetServers().First();

        IAsyncEnumerable<RedisKey> asyncEnum = server.KeysAsync(_dbNumber, pattern: pattern);

        await foreach (RedisKey redisKey in asyncEnum)
        {
            string? countryJson = await _database.StringGetAsync(redisKey);

            Country? country = JsonSerializer.Deserialize<Country?>(countryJson!);

            _countryPolygons[country!.Name] = country.GeoFence;
        }
    }
}