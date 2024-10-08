using GeospatialWeb.Geography;
using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class PoiServiceEF(ApplicationDbContext _dbContext) : PoiServiceBase
{
    private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(ApplicationDbContext.SRID);

    public override IAsyncEnumerable<PoiResponse> FindPoisDistance(PoiRequest poiRequest, CancellationToken ct = default)
    {
        Point point = _geometryFactory.CreatePoint(poiRequest.Lng, poiRequest.Lat);

        return findPois(poiRequest.Lng, poiRequest.Lat, poi => poi.Location.Distance(point) <= poiRequest.Distance, ct);
    }

    public override IAsyncEnumerable<PoiResponse> FindPoisWithin(PoiRequestWithin poiRequest, CancellationToken ct = default)
    {
        Coordinate[] coordinates = toPolygonCoordinates(poiRequest, coordinate => new Coordinate(coordinate.Lng, coordinate.Lat));

        var polygon = _geometryFactory.CreatePolygon(coordinates);

        return findPois(poiRequest.CenterLng, poiRequest.CenterLat, poi => poi.Location.CoveredBy(polygon), ct);
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

        Guid countryId = Guid.Parse(countryName);

        IAsyncEnumerable<PoiData> asyncEnum = _dbContext.POIs
            .Where(poi => poi.CountryId == countryId)
            .Where(wherePredicate)
            .AsAsyncEnumerable();

        await foreach (PoiData poi in asyncEnum.WithCancellation(ct))
        {
            (double poiLat, double poiLng) = poi.Location.GetLatLng();

            double distance = GeoUtils.HaversineDistance(poiLng, poiLat, centerLng, centerLat);

            yield return new PoiResponse(poi.Id, poi.Name, poi.Category, poiLng, poiLat, distance);
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

    public static (double latitude, double longitude) GetLatLng(this Point point)
    {
        return (point.Y, point.X);
    }
}
