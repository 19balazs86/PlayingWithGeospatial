using GeospatialWeb.Services.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Spatial;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.CosmosDB;

public sealed class PoiServiceCosmos(CosmosClient _cosmosClient) : IPoiService
{
    private const string _databaseName = "PlayingWith_Geospatial";

    private Database _database => _cosmosClient.GetDatabase(_databaseName);

    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lat, poiRequest.Lng, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(countryName) };

        var point = new Point(poiRequest.Lng, poiRequest.Lat);

        Container container = _database.GetContainer(PoiData.ContainerId);

        using FeedIterator<PoiData> feedIterator = container
            .GetItemLinqQueryable<PoiData>(requestOptions: requestOptions)
            .Where(poi => poi.Location.Distance(point) <= poiRequest.Distance)
            // Lat and Lng do not get populated
            // .Select(poi => new PoiResponse(poi.Id, poi.Name, poi.Category, poi.Location.Position.Latitude, poi.Location.Position.Longitude)
            .ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            foreach (PoiData poi in await feedIterator.ReadNextAsync(ct))
            {
                yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poi.Location.Position.Latitude, poi.Location.Position.Longitude);
            }
        }
    }

    public async Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default)
    {
        Container container = _database.GetContainer(Country.ContainerId);

        var point = new Point(longitude, latitude);

        using FeedIterator<string> feedIterator = container
            .GetItemLinqQueryable<Country>()
            .Where(c => c.GeoFence.Intersects(point))
            .Select(c => c.Name)
            .ToFeedIterator();

        if (feedIterator.HasMoreResults)
        {
            FeedResponse<string> feedResponse = await feedIterator.ReadNextAsync(ct);

            return feedResponse.FirstOrDefault();
        }

        return null;
    }

    public async Task DatabaseSeed(CancellationToken ct = default)
    {
        DatabaseResponse response = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName, cancellationToken: ct);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            await databaseSeed_Country(response.Database, "France",  ct);
            await databaseSeed_Country(response.Database, "Spain",   ct);
            await databaseSeed_Country(response.Database, "Ireland", ct);

            await databaseSeed_POIs(response.Database, "France",  ct);
            await databaseSeed_POIs(response.Database, "Spain",   ct);
            await databaseSeed_POIs(response.Database, "Ireland", ct);
        }
    }

    private static async Task databaseSeed_Country(Database database, string countryName, CancellationToken ct = default)
    {
        var properties = createContainerProperties(Country.ContainerId, Country.PartitionKeyPath, Country.SpatialPath, SpatialType.Polygon);

        Container container = await database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct);

        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Country.json"));

        CountrySeedRecord[]? seedRecords = await JsonSerializer.DeserializeAsync<CountrySeedRecord[]?>(fileStream, cancellationToken: ct);

        List<Position> geoFencePoints = seedRecords!.Select(c => new Position(c.Lng, c.Lat)).ToList();

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = new Polygon(geoFencePoints)
        };

        await container.CreateItemAsync(country, cancellationToken: ct);
    }

    private static async Task databaseSeed_POIs(Database database, string countryName, CancellationToken ct = default)
    {
        var properties = createContainerProperties(PoiData.ContainerId, PoiData.PartitionKeyPath, PoiData.SpatialPath, SpatialType.Point);

        Container container = await database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct);

        TransactionalBatch batch = container.CreateTransactionalBatch(new PartitionKey(countryName));

        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Poi.json"));

        var asyncEnum = JsonSerializer.DeserializeAsyncEnumerable<PoiSeedRecord?>(fileStream, cancellationToken: ct);

        await foreach (PoiSeedRecord? poiSeedRecord in asyncEnum)
        {
            var poiData = new PoiData
            {
                Id          = Guid.NewGuid(),
                CountryName = countryName,
                Category    = poiSeedRecord!.Category,
                Name        = poiSeedRecord.Name,
                Location    = new Point(poiSeedRecord.Lng, poiSeedRecord.Lat)
            };

            batch.CreateItem(poiData);
        }

        await batch.ExecuteAsync(ct);
    }

    private static ContainerProperties createContainerProperties(string id, string partitionKeyPath, string spatialPath, SpatialType spatialType)
    {
        var sp = new SpatialPath { Path = spatialPath };

        sp.SpatialTypes.Add(spatialType);

        var indexingPolicy = new IndexingPolicy();

        indexingPolicy.SpatialIndexes.Add(sp);

        return new ContainerProperties
        {
            Id               = id,
            PartitionKeyPath = partitionKeyPath,
            IndexingPolicy   = indexingPolicy
        };
    }
}