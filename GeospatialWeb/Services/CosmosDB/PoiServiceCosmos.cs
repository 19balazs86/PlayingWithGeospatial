using GeospatialWeb.Geography;
using GeospatialWeb.Services.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Spatial;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;

namespace GeospatialWeb.Services.CosmosDB;

public sealed class PoiServiceCosmos(CosmosClient _cosmosClient) : PoiServiceBase
{
    private const string _databaseName = "PlayingWith_Geospatial";

    private Database _database => _cosmosClient.GetDatabase(_databaseName);

    public override IAsyncEnumerable<PoiResponse> FindPoisDistance(PoiRequest poiRequest, CancellationToken ct = default)
    {
        var point = new Point(poiRequest.Lng, poiRequest.Lat);

        return findPois(poiRequest.Lng, poiRequest.Lat, poi => poi.Location.Distance(point) <= poiRequest.Distance, ct);
    }

    public override IAsyncEnumerable<PoiResponse> FindPoisWithin(PoiRequestWithin poiRequest, CancellationToken ct = default)
    {
        Position[] positions =
        [
            new Position(poiRequest.NWLng, poiRequest.NWLat),
            new Position(poiRequest.SWLng, poiRequest.SWLat),
            new Position(poiRequest.SELng, poiRequest.SELat),
            new Position(poiRequest.NELng, poiRequest.NELat),
            new Position(poiRequest.NWLng, poiRequest.NWLat)
        ];

        var polygon = new Polygon(positions);

        return findPois(poiRequest.CenterLng, poiRequest.CenterLat, poi => poi.Location.Within(polygon), ct);
    }

    private async IAsyncEnumerable<PoiResponse> findPois(
        double centerLng,
        double centerLat,
        Expression<Func<PoiData, bool>> wherePredicate,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string? countryName = await FindCountryName(centerLng, centerLat, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(countryName) };

        Container container = _database.GetContainer(PoiData.ContainerId);

        using var feedIterator = container
            .GetItemLinqQueryable<PoiData>(requestOptions: requestOptions)
            .Where(wherePredicate)
            .ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            foreach (PoiData poi in await feedIterator.ReadNextAsync(ct))
            {
                (double poiLat, double poiLng) = poi.Location.GetLatLng();

                double distance = GeoUtils.HaversineDistance(poiLng, poiLat, centerLng, centerLat);

                yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poiLng, poiLat, distance);
            }
        }
    }

    public override async Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default)
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

    public override async Task DatabaseSeed(CancellationToken ct = default)
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
        CountrySeedRecord[] seedRecords = await getCountrySeedRecords(countryName, ct);

        List<Position> geoFencePoints = seedRecords.Select(c => new Position(c.Lng, c.Lat)).ToList();

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = new Polygon(geoFencePoints)
        };

        var properties = createContainerProperties(Country.ContainerId, Country.PartitionKeyPath, Country.SpatialPath, SpatialType.Polygon);

        Container container = await database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct);

        await container.CreateItemAsync(country, cancellationToken: ct);
    }

    private static async Task databaseSeed_POIs(Database database, string countryName, CancellationToken ct = default)
    {
        var properties = createContainerProperties(PoiData.ContainerId, PoiData.PartitionKeyPath, PoiData.SpatialPath, SpatialType.Point);

        Container container = await database.CreateContainerIfNotExistsAsync(properties, cancellationToken: ct);

        TransactionalBatch batch = container.CreateTransactionalBatch(new PartitionKey(countryName));

        await foreach (PoiSeedRecord poiSeedRecord in getPoiSeedRecords(countryName, ct))
        {
            var poiData = new PoiData
            {
                Id          = Guid.NewGuid(),
                CountryName = countryName,
                Category    = poiSeedRecord.Category,
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

file static class GeometryUtils
{
    public static (double latitude, double longitude) GetLatLng(this Point point)
    {
        return (point.Position.Latitude, point.Position.Longitude);
    }
}