﻿using GeospatialWeb.Services.MongoDB.Models;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.MongoDB;

public sealed class PoiServiceMongo(MongoClient _mongoClient) : IPoiService
{
    private const string _databaseName = "PlayingWith_Geospatial";

    private IMongoDatabase _database => _mongoClient.GetDatabase(_databaseName);

    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lat, poiRequest.Lng, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        var point = GeoJson.Point(new GeoJson2DGeographicCoordinates(poiRequest.Lng, poiRequest.Lat));

        var nearFilter     = Builders<PoiData>.Filter.Near(p => p.Location, point, poiRequest.Distance);
        var whereFilter    = Builders<PoiData>.Filter.Where(p => p.CountryName == countryName);
        var combinedFilter = Builders<PoiData>.Filter.And(nearFilter, whereFilter);

        IAsyncCursor<PoiData> asyncCursor = await _database.GetCollection<PoiData>(PoiData.CollectionName)
            .FindAsync(combinedFilter, cancellationToken: ct);

        while (await asyncCursor.MoveNextAsync(ct))
        {
            foreach (PoiData poi in asyncCursor.Current)
            {
                yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poi.Location.Coordinates.Latitude, poi.Location.Coordinates.Longitude);
            }
        }
    }

    public async Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default)
    {
        var point = GeoJson.Point(new GeoJson2DGeographicCoordinates(longitude, latitude));

        var filter = Builders<Country>.Filter.GeoIntersects(c => c.GeoFence, point);

        return await _database.GetCollection<Country>(Country.CollectionName)
            .Find(filter)
            .Project(c => c.Name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task DatabaseSeed(CancellationToken ct = default)
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
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Country.json"));

        CountrySeedRecord[]? seedRecords = await JsonSerializer.DeserializeAsync<CountrySeedRecord[]?>(fileStream, cancellationToken: ct);

        var geoFence_Coordinates = seedRecords!.Select(c => new GeoJson2DGeographicCoordinates(c.Lng, c.Lat)).ToArray();

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
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Poi.json"));

        var asyncEnum = JsonSerializer.DeserializeAsyncEnumerable<PoiSeedRecord?>(fileStream, cancellationToken: ct);

        List<PoiData> pois = [];

        await foreach (PoiSeedRecord? poiSeedRecord in asyncEnum)
        {
            var poiData = new PoiData
            {
                Id          = Guid.NewGuid(),
                CountryName = countryName,
                Category    = poiSeedRecord!.Category,
                Name        = poiSeedRecord.Name,
                Location    = GeoJson.Point(new GeoJson2DGeographicCoordinates(poiSeedRecord.Lng, poiSeedRecord.Lat))
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