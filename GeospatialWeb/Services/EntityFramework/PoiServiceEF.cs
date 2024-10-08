using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Runtime.CompilerServices;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class PoiServiceEF(ApplicationDbContext _dbContext) : PoiServiceBase
{
    private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(ApplicationDbContext.SRID);

    public override async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lng, poiRequest.Lat, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        Point point = _geometryFactory.CreatePoint(poiRequest.Lng, poiRequest.Lat);

        Guid countryId = Guid.Parse(countryName);

        // The distance is calculated twice, even for an entity with the Distance field populated using Location.Distance(point) and a filter applied to the Distance field
        var asyncEnum = _dbContext.POIs
            .Where(poi => poi.CountryId == countryId && poi.Location.Distance(point) <= poiRequest.Distance)
            .Select(poi => new { Entity = poi, Distance = poi.Location.Distance(point) })
            .AsAsyncEnumerable();

        await foreach (var item in asyncEnum)
        {
            PoiData poi = item.Entity;

            yield return new PoiResponse(poi.Id, poi.Name, poi.Category, Lng: poi.Location.X, Lat: poi.Location.Y, Distance: item.Distance);
        }
    }

    public override async Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default)
    {
        Point point = _geometryFactory.CreatePoint(longitude, latitude);

        return await _dbContext.Countries
            .Where(c => c.GeoFence.Covers(point))
            .Select(c => c.Id.ToString())
            .FirstOrDefaultAsync(ct);

        // This does not work: ST_Contains does not work with geography type
        // return _dbContext.Countries.Where(c => c.GeoFence.Contains(point)).FirstOrDefaultAsync(ct);

        // It works by casting the GeoFence to a geometry
        //string sql =
        //    $$"""
        //    SELECT "Id"::text AS "Value"
        //    FROM "Countries"
        //    WHERE ST_Contains("GeoFence"::geometry, ST_SetSRID(ST_MakePoint({0}, {1}), {{ApplicationDbContext.SRID}}))
        //    """;

        //FormattableString formattedSql = FormattableStringFactory.Create(sql, longitude, latitude);

        //return await _dbContext.Database.SqlQuery<string?>(formattedSql).FirstOrDefaultAsync(ct);
    }

    public override async Task DatabaseSeed(CancellationToken ct = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(ct);

        bool isDatabaseExists = await _dbContext.Countries.AnyAsync(ct);

        if (isDatabaseExists)
        {
            return;
        }

        Guid countryFrance  = await databaseSeed_Country("France",  ct);
        Guid countrySpain   = await databaseSeed_Country("Spain",   ct);
        Guid countryIreland = await databaseSeed_Country("Ireland", ct);

        await databaseSeed_POIs(countryFrance,  "France",  ct);
        await databaseSeed_POIs(countrySpain,   "Spain",   ct);
        await databaseSeed_POIs(countryIreland, "Ireland", ct);
    }

    private async Task<Guid> databaseSeed_Country(string countryName, CancellationToken ct = default)
    {
        CountrySeedRecord[] seedRecords = await getCountrySeedRecords(countryName, ct);

        Coordinate[] coordinates = seedRecords.Select(c => new Coordinate(c.Lng, c.Lat)).ToArray();

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = _geometryFactory.CreatePolygon(coordinates)
        };

        await _dbContext.Countries.AddAsync(country, ct);

        await _dbContext.SaveChangesAsync(ct);

        return country.Id;
    }

    private async Task databaseSeed_POIs(Guid countryId, string countryName, CancellationToken ct = default)
    {
        await foreach (PoiSeedRecord poiSeedRecord in getPoiSeedRecords(countryName, ct))
        {
            var poiData = new PoiData
            {
                Id        = Guid.NewGuid(),
                CountryId = countryId,
                Category  = poiSeedRecord.Category,
                Name      = poiSeedRecord.Name,
                Location  = _geometryFactory.CreatePoint(poiSeedRecord.Lng, poiSeedRecord.Lat)
            };

            await _dbContext.POIs.AddAsync(poiData, ct);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}

file static class GeometryFactoryExtension
{
    public static Point CreatePoint(this GeometryFactory factory, double longitude, double latitude)
    {
        return factory.CreatePoint(new Coordinate(longitude, latitude));
    }

    public static Polygon CreatePolygon(this GeometryFactory factory, Coordinate[] coordinates)
    {
        return factory.CreatePolygon(factory.CreateLinearRing(coordinates));
    }
}
