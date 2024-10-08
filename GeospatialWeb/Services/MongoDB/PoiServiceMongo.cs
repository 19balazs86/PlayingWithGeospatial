using GeospatialWeb.Geography;
using GeospatialWeb.Services.MongoDB.Models;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace GeospatialWeb.Services.MongoDB;

public sealed class PoiServiceMongo(MongoClient _mongoClient) : PoiServiceBase
{
    private const string _databaseName = "PlayingWith_Geospatial";

    private IMongoDatabase _database => _mongoClient.GetDatabase(_databaseName);

    public override IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, CancellationToken ct = default)
    {
        var point = GeoJsonUtils.Point(poiRequest.Lng, poiRequest.Lat);

        var nearFilter  = Builders<PoiData>.Filter.Near(p  => p.Location, point, poiRequest.Distance);

        return findPois(poiRequest.Lng, poiRequest.Lat, nearFilter, ct);
    }

    public override IAsyncEnumerable<PoiResponse> FindPoisWithin(PoiRequestWithin poiRequest, CancellationToken ct = default)
    {
        GeoJson2DGeographicCoordinates[] coordinates =
        [
            new GeoJson2DGeographicCoordinates(poiRequest.NWLng, poiRequest.NWLat),
            new GeoJson2DGeographicCoordinates(poiRequest.SWLng, poiRequest.SWLat),
            new GeoJson2DGeographicCoordinates(poiRequest.SELng, poiRequest.SELat),
            new GeoJson2DGeographicCoordinates(poiRequest.NELng, poiRequest.NELat),
            new GeoJson2DGeographicCoordinates(poiRequest.NWLng, poiRequest.NWLat)
        ];

        var polygon = GeoJson.Polygon(GeoJson.PolygonCoordinates((coordinates)));

        var withinFilter = Builders<PoiData>.Filter.GeoWithin(p => p.Location, polygon);
        // var withinFilter = Builders<PoiData>.Filter.GeoWithinBox(p => p.Location, poiRequest.SWLng, poiRequest.SWLat, poiRequest.NELng, poiRequest.NELat);
        // var withinFilter = Builders<PoiData>.Filter.GeoWithinCenter(p => p.Location, x, y, radius);
        // var withinFilter = Builders<PoiData>.Filter.GeoWithinPolygon(p => p.Location, points);

        return findPois(poiRequest.CenterLng, poiRequest.CenterLat, withinFilter, ct);
    }

    private async IAsyncEnumerable<PoiResponse> findPois(
        double centerLng,
        double centerLat,
        FilterDefinition<PoiData> poiFilter,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string? countryName = await FindCountryName(centerLng, centerLat, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        var whereFilter = Builders<PoiData>.Filter.Where(p => p.CountryName == countryName);
        var andFilter   = Builders<PoiData>.Filter.And(whereFilter, poiFilter);

        IAsyncCursor<PoiData> asyncCursor = await _database.GetCollection<PoiData>(PoiData.CollectionName)
            .FindAsync(andFilter, cancellationToken: ct);

        while (await asyncCursor.MoveNextAsync(ct))
        {
            foreach (PoiData poi in asyncCursor.Current)
            {
                (double poiLat, double poiLng) = poi.Location.GetLatLng();

                double distance = GeoUtils.HaversineDistance(poiLng, poiLat, centerLng, centerLat);

                yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poiLng, poiLat, distance);
            }
        }
    }

    public override async Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default)
    {
        var point = GeoJsonUtils.Point(longitude, latitude);

        var filter = Builders<Country>.Filter.GeoIntersects(c => c.GeoFence, point);

        return await _database.GetCollection<Country>(Country.CollectionName)
            .Find(filter)
            .Project(c => c.Name)
            .FirstOrDefaultAsync(ct);
    }

    public override async Task DatabaseSeed(CancellationToken ct = default)
    {
        using IAsyncCursor<string> asyncCursor = await _mongoClient.ListDatabaseNamesAsync(ct);

        List<string> databaseNames = await asyncCursor.ToListAsync(ct);

        bool isDatabaseExists = databaseNames.Contains(_databaseName);

        if (isDatabaseExists)
        {
            return;
        }

        IMongoCollection<Country> countryCollection = await getCollectionWithIndex<Country>(Country.CollectionName, c => c.GeoFence, null, ct);

        await databaseSeed_Country(countryCollection, "France",  ct);
        await databaseSeed_Country(countryCollection, "Spain",   ct);
        await databaseSeed_Country(countryCollection, "Ireland", ct);

        IMongoCollection<PoiData> poiCollection = await getCollectionWithIndex<PoiData>(PoiData.CollectionName, p => p.Location, p => p.CountryName, ct);

        await databaseSeed_POIs(poiCollection, "France",  ct);
        await databaseSeed_POIs(poiCollection, "Spain",   ct);
        await databaseSeed_POIs(poiCollection, "Ireland", ct);
    }

    private static async Task databaseSeed_Country(IMongoCollection<Country> collection, string countryName, CancellationToken ct = default)
    {
        CountrySeedRecord[] seedRecords = await getCountrySeedRecords(countryName, ct);

        var geoFence_Coordinates = seedRecords.Select(c => new GeoJson2DGeographicCoordinates(c.Lng, c.Lat)).ToArray();

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = GeoJson.Polygon(GeoJson.PolygonCoordinates(geoFence_Coordinates))
        };

        await collection.InsertOneAsync(country, cancellationToken: ct);
    }

    private static async Task databaseSeed_POIs(IMongoCollection<PoiData> collection, string countryName, CancellationToken ct = default)
    {
        List<PoiData> pois = [];

        await foreach (PoiSeedRecord poiSeedRecord in getPoiSeedRecords(countryName, ct))
        {
            var poiData = new PoiData
            {
                Id          = Guid.NewGuid(),
                CountryName = countryName,
                Category    = poiSeedRecord.Category,
                Name        = poiSeedRecord.Name,
                Location    = GeoJsonUtils.Point(poiSeedRecord.Lng, poiSeedRecord.Lat)
            };

            pois.Add(poiData);
        }

        await collection.InsertManyAsync(pois, cancellationToken: ct);
    }

    private async Task<IMongoCollection<TEntity>> getCollectionWithIndex<TEntity>(
        string collectionName,
        Expression<Func<TEntity, object>> geoIndexExp,
        Expression<Func<TEntity, object>>? additionalIndexExp = null,
        CancellationToken ct = default)
    {
        IMongoCollection<TEntity> countryCollection = _database.GetCollection<TEntity>(collectionName);

        var geoIndexModel = new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Geo2DSphere(geoIndexExp));

        List<CreateIndexModel<TEntity>> indexes = [geoIndexModel];

        if (additionalIndexExp is not null)
        {
            var additionalIndexModel = new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(additionalIndexExp));

            indexes.Add(additionalIndexModel);
        }

        await countryCollection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);

        return countryCollection;
    }
}

file static class GeoJsonUtils
{
    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> Point(double longitude, double latitude)
    {
        return GeoJson.Point(new GeoJson2DGeographicCoordinates(longitude, latitude));
    }

    public static (double latitude, double longitude) GetLatLng(this GeoJsonPoint<GeoJson2DGeographicCoordinates> point)
    {
        return (point.Coordinates.Latitude, point.Coordinates.Longitude);
    }
}

//public async IAsyncEnumerable<PoiResponse> FindPOI_using_BsonDocument(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
//{
//    string? countryName = await FindCountryName(poiRequest.Lng, poiRequest.Lat, ct);

//    if (string.IsNullOrEmpty(countryName))
//    {
//        yield break;
//    }

// No need to define the Location field for the $geoNear filter, as Mongo uses the indexed field by default
//    var geoNearStage = new BsonDocument
//    {
//        { "$geoNear", new BsonDocument
//            {
//                { "near", new BsonDocument
//                    {
//                        { "type", "Point" },
//                        { "coordinates", new BsonArray { poiRequest.Lng, poiRequest.Lat } }
//                    }
//                },
//                { "distanceField", "distance" },
//                { "maxDistance", poiRequest.Distance },
//                { "query", new BsonDocument
//                    {
//                        { "CountryName", countryName }
//                    }
//                },
//                { "spherical", true }
//            }
//        }
//    };

//    var pipeline = new[] { geoNearStage };

//    IAsyncCursor<BsonDocument> asyncCursor = await _database
//        .GetCollection<BsonDocument>(PoiData.CollectionName)
//        .AggregateAsync<BsonDocument>(pipeline, cancellationToken: ct);

//    while (await asyncCursor.MoveNextAsync(ct))
//    {
//        foreach (BsonDocument document in asyncCursor.Current)
//        {
//            double distance = document["distance"].ToDouble();

//            document.Remove("distance"); // Need to remove it, because error: Element 'distance' does not match any field or property of class PoiData

//            PoiData poi = BsonSerializer.Deserialize<PoiData>(document);

//            (double poiLat, double poiLng) = poi.Location.GetLatLng();

//            yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poiLng, poiLat, distance);
//        }
//    }
//}