using GeospatialWeb.Geography;
using GeospatialWeb.Services.Redis.Models;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.Redis;

public sealed class PoiServiceRedis(IConnectionMultiplexer _connectionMultiplexer) : IPoiService
{
    private const int _dbNumber = 0;

    private readonly IDatabase _database = _connectionMultiplexer.GetDatabase(_dbNumber);

    private LoadedLuaScript? _loadedLuaScript;

    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lng, poiRequest.Lat, ct);

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

            yield return new PoiResponse(poiData!.Id, poiData.Name, poiData.Category, poiData.Location.Lng, poiData.Location.Lat, result.Distance.GetValueOrDefault(-1));
        }
    }

    public async Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default)
    {
        var scriptParams = new
        {
            keyPrefix = (RedisKey)$"{Country.GeoFencePolygonKey}",
            pointLng  = longitude,
            pointLat  = latitude
        };

        RedisResult result = await _loadedLuaScript!.EvaluateAsync(_database, scriptParams);

        return result.ToString().Replace($"{Country.GeoFencePolygonKey}:", string.Empty);
    }

    public async Task DatabaseSeed(CancellationToken ct = default)
    {
        _loadedLuaScript = await inicializeRayCastingLuaScript();

        bool isDatabaseExists = await _database.KeyExistsAsync(PoiData.GetGeoIdKey("France"));

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

        var geoFence_Coordinates = seedRecords!.Select(c => new GeoLocation(c.Lng, c.Lat)).ToList();

        var geoFence = new GeoPolygon(geoFence_Coordinates);

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = geoFence
        };

        string countryJson = JsonSerializer.Serialize(country, CountrySerializationContext.Default.Country);

        await _database.StringSetAsync(country.GetIdKey(), countryJson);

        await country.StoreGeoFence(_database);
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
                Location = new GeoLocation(poiSeedRecord.Lng, poiSeedRecord.Lat)
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

    private async Task<LoadedLuaScript> inicializeRayCastingLuaScript()
    {
        LuaScript.PurgeCache();

        IServer server = _connectionMultiplexer.GetServers().First();

        return await LuaScriptDefinitions.PreparedRayCastingLuaScript.LoadAsync(server);
    }

    //private async Task inicializeCountryPolygons()
    //{
    //    IServer server = _connectionMultiplexer.GetServers().First();
    //    IAsyncEnumerable<RedisKey> asyncEnum = server.KeysAsync(_dbNumber, pattern: "KeyPrefix*");
    //    await foreach (RedisKey redisKey in asyncEnum)
    //    {
    //        // ...
    //    }
    //}
}

file static class CountryExtensions
{
    // Each GeoFence coordinate is stored in a hash key as 2 bytes: (byte) longitude and (byte) latitude
    // Bytes take up less space than text, and Redis performs better with struct.unpack on bytes than using tonumber on text
    public static async Task StoreGeoFence(this Country country, IDatabase database)
    {
        var points = country.GeoFence.Points;

        for (int i = 0; i < points.Count; i++)
        {
            GeoLocation geoLocation = points[i];

            byte[] bytesToStore = convertToBytes(geoLocation.Lng, geoLocation.Lat);

            RedisKey key = $"{Country.GeoFencePolygonKey}:{country.Name}";

            RedisValue field = (i + 1);

            await database.HashSetAsync(key, field, bytesToStore);
        }
    }

    private static byte[] convertToBytes(double[] coordinates)
    {
        int totalLength = coordinates.Length * sizeof(double);

        byte[] binaryData = new byte[totalLength];

        Buffer.BlockCopy(coordinates, 0, binaryData, 0, totalLength);

        return binaryData;
    }

    private static byte[] convertToBytes(double lng, double lat)
    {
        return convertToBytes([lng, lat]);
    }
}